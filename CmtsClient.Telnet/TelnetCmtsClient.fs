namespace KD.CmtsClient.Telnet

open System
open System.Net
open System.Threading.Tasks

open KD.CmtsClient
open KD.Telnet.TcpTelnetClient


type TelnetCmtsClient(ip: IPAddress) = 
    let doneBytes: byte[] = [| 0x0Duy; 0x00uy;|]
    let client = new TcpTelnetClient() :> ITcpTelnetClient

    let ClearDuplicatesIpV4 timeout =
        task {
            do! client.SendDataReceiveEcho("clear arp-cache", timeout) |> Task.Ignore
            do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

            let! response = client.ReceiveData(timeout)
            if not (response.Contains("You are about to delete all ARP cache entries!")) then
                return false
            else
                do! client.SendDataReceiveEcho("yes", timeout) |> Task.Ignore
                do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

                let! secondResponse = client.ReceiveData(timeout)
                return secondResponse.EndsWith("#")
            }

    let ClearDuplicatesIpV6 timeout =
        task {
            do! client.SendDataReceiveEcho("clear ipv6 neighbors bundle 10", timeout) |> Task.Ignore
            do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

            let! response = client.ReceiveData(timeout)
            return response.EndsWith("#")
            }

    interface ICmtsClient with
        member _.IsConnected() = client.IsConnected()

        member _.SendKeepAlive timeout =
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

        member _.ConnectAndLogin username password enPassword timeout =
            if String.IsNullOrWhiteSpace username then
                raise (ArgumentNullException(nameof username))
            if String.IsNullOrWhiteSpace password then
                raise (ArgumentNullException(nameof password))
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            let await t = Async.AwaitTask t
            let awaitTask (t: Task) = Async.AwaitTask t

            let (|EndsWith|_|) (ends: string) (str: string) =
                if str.EndsWith(ends) 
                then Some()
                else None

            let (|Contains|_|) (contains: string) (str: string) =
                if str.Contains(contains)
                then Some()
                else None

            let rec handleLogin (response: string) = async {
                match response.Trim() with
                | EndsWith "#"              ->
                    return true
                | Contains "Bad passwords"  ->
                    return false
                | EndsWith ">"              ->
                    do! await <| client.SendDataReceiveEcho("en", timeout) |> Async.Ignore
                    do! await <| client.SendDataReceiveEcho(doneBytes, timeout) |> Async.Ignore
                    let! nextResponse = await <| client.ReceiveData timeout
                    if not (nextResponse.Trim().EndsWith("Password:")) then
                        return false
                    else
                        do! awaitTask <| client.SendData enPassword
                        do! await <| client.SendDataReceiveEcho(doneBytes, timeout) |> Async.Ignore
                        let! nextResponse = await <| client.ReceiveData timeout
                        return! handleLogin nextResponse
                | EndsWith "Username:"      ->
                    do! await <| client.SendDataReceiveEcho (username, timeout) |> Async.Ignore
                    do! await <| client.SendDataReceiveEcho(doneBytes, timeout) |> Async.Ignore
                    let! nextResponse = await <| client.ReceiveData timeout
                    return! handleLogin nextResponse
                | EndsWith "Password:"      ->
                    do! awaitTask <| client.SendData password
                    do! await <| client.SendDataReceiveEcho(doneBytes, timeout) |> Async.Ignore
                    let! nextResponse = await <| client.ReceiveData timeout
                    return! handleLogin nextResponse
                | _                         ->
                    return false
            }

            task {
                do! client.ConnectAsync ip 23
                let! firstResponse = client.ReceiveData timeout
                return! handleLogin firstResponse
            }

        member _.ClearDuplicatesIpV4 timeout = 
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            ClearDuplicatesIpV4 timeout

        member __.ClearDuplicatesIpV6 timeout =
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            ClearDuplicatesIpV6 timeout

        member _.ClearDuplicates isIpv6 timeout =
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            if (isIpv6) then
                ClearDuplicatesIpV6 timeout
            else
                ClearDuplicatesIpV4 timeout

        //arg could be a mac to get specifically the info for that mac or empty to get all the ipv4 info or ipv6 to see all the ipv6 info
        member _.ShowCableModem arg timeout = task {
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            let command =
                if String.IsNullOrWhiteSpace arg then
                    "scm"
                else
                    $"scm {arg}"
            do! client.SendDataReceiveEcho(command, timeout) |> Task.Ignore
            do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

            return! client.ReceiveData(timeout)
            }

        member _.ShowLogging mac timeout = task {
            if String.IsNullOrWhiteSpace mac then
                raise (ArgumentNullException(nameof mac))
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            do! client.SendDataReceiveEcho($"sh logging | i {mac}", timeout) |> Task.Ignore
            do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

            return! client.ReceiveData(timeout)
            }

        member __.Dispose() =
            client.Dispose()
