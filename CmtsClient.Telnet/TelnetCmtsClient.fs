namespace KD.CmtsClient.Telnet

open FSharp.Control.Tasks.V2.ContextInsensitive
open KD.Telnet.TcpTelnetClient
open System.Threading.Tasks
open System

type TelnetCmtsClient() = 
    let (>>=!) (xa : unit -> Task<bool>) ca = fun() ->
        task {
            match! xa() with
            | false -> return false
            | true -> return! ca()
        }

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

    interface ITelnetCmtsClient with
        member _.ConnectAsync ip =  client.ConnectAsync ip 23

        member _.Login password enPassword timeout =
            let sendPassword (endsWithString:string) =
                task {
                    do! client.SendData password
                    do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore
                    let! response = client.ReceiveData timeout
                    return response.Trim().EndsWith endsWithString
                    }

            let sendEnable (endsWithString: string) =
                task {
                    do! client.SendDataReceiveEcho("en", timeout)  |> Task.Ignore
                    do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore
                    let! response = client.ReceiveData timeout
                    return response.Trim().EndsWith endsWithString
                }

            let sendEnablePassword (endsWithString: string) =
                task {
                    do! client.SendData enPassword
                    do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore
                    let! response = client.ReceiveData timeout
                    return response.Trim().EndsWith endsWithString
                }

            task {
                let! firstResponse = client.ReceiveData timeout
                let firstResponseTrimmed = firstResponse.Trim()
                if firstResponseTrimmed.EndsWith("#") then
                    return true
                else if firstResponseTrimmed.EndsWith("Password:") then
                    let taskChain = (fun () -> sendPassword ">" ) >>=! (fun () -> sendEnable "Password:") >>=! (fun () -> sendEnablePassword "#")
                    return! taskChain()
                else if (firstResponseTrimmed.EndsWith("Username:")) then
                    let taskChain = (fun () -> sendPassword "Password:") >>=! (fun () -> sendEnablePassword "#")
                    return! taskChain()
                else 
                    return false
                }

        member _.ClearDuplicatesIpV4 timeout = ClearDuplicatesIpV4 timeout

        member __.ClearDuplicatesIpV6 timeout = ClearDuplicatesIpV6 timeout
            
        member _.ClearDuplicates isIpv6 timeout = 
            if (isIpv6) then
                ClearDuplicatesIpV6 timeout
            else
                ClearDuplicatesIpV4 timeout

        member _.ShowCableModem mac timeout = task {
            do! client.SendDataReceiveEcho((sprintf "scm %s" mac), timeout) |> Task.Ignore
            do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

            return! client.ReceiveData(timeout)
            }

        member _.ShowLogging mac timeout = task {
            do! client.SendDataReceiveEcho((sprintf "sh logging | i %s" mac), timeout) |> Task.Ignore
            do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

            return! client.ReceiveData(timeout)
            }

        member __.Dispose() = 
            client.Dispose()