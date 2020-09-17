namespace KD.CmtsClient.Telnet

open FSharp.Control.Tasks.V2.ContextInsensitive
open KD.Telnet
open KD.Telnet.TcpTelnetClient
open System.Threading.Tasks

type TelnetCmtsClient() = 
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
            let sendPassword() =
                task {
                    do! client.SendData password
                    do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

                    let! secondResponse = client.ReceiveData(timeout)
                    return secondResponse.EndsWith(">")
                    }

            let sendEnable() =
                task {
                    do! client.SendDataReceiveEcho("en", timeout)  |> Task.Ignore
                    do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore

                    let! thirdResponse = client.ReceiveData(timeout)
                    return thirdResponse.Contains("Password")
                }

            let sendEnablePassword() =
                task {
                    do! client.SendData enPassword
                    do! client.SendDataReceiveEcho(doneBytes, timeout) |> Task.Ignore
                
                    let! forthResponse = client.ReceiveData(timeout)
                    return forthResponse.EndsWith("#")
                }

            task {
                let! firstResponse = client.ReceiveData(timeout)
                if not (firstResponse.Contains("Password")) then
                    return firstResponse.EndsWith("#")
                else
                    let taskChain = sendPassword >>=! sendEnable >>=! sendEnablePassword
                    return! taskChain()
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