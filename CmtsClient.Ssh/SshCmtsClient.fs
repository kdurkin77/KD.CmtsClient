namespace KD.CmtsClient.Ssh

open System.Net
open System.Threading
open System.Threading.Tasks

open Renci.SshNet

open KD.CmtsClient


type SshCmtsClient(ip: IPAddress) = 
    let mutable client = new SshClient(ip.ToString(), "admin")

    let executeCommand command ct =
        task {
            use cmd = client.CreateCommand(command)
            do! cmd.ExecuteAsync(ct)
            return cmd.Result
        }

    let ClearDuplicatesIpV4 ct =
        task {
            let! response1 = executeCommand "clear arp-cache" ct
            if response1.Contains("You are about to delete all ARP cache entries!") then
                let! response2 = executeCommand "yes" ct
                return response2.EndsWith("#")
            else
                return false
        }

    let ClearDuplicatesIpV6 ct =
        task {
            let! response = executeCommand "clear ipv6 neighbors bundle 10" ct
            return response.EndsWith("#")
        }

    interface ICmtsClient with
        member _.IsConnected() = client.IsConnected

        member _.SendKeepAlive timeout =
            use cts = new CancellationTokenSource(timeout)
            executeCommand "" cts.Token :> Task

        member _.ConnectAndLogin username password _ timeout =
            task {
                client <- new SshClient(ip.ToString(), username, password)
                use cts = new CancellationTokenSource(timeout)
                do! client.ConnectAsync(cts.Token)
                return true
            }

        member _.ClearDuplicatesIpV4 timeout =
            use cts = new CancellationTokenSource(timeout)
            ClearDuplicatesIpV4 cts.Token

        member _.ClearDuplicatesIpV6 timeout =
            use cts = new CancellationTokenSource(timeout)
            ClearDuplicatesIpV6 cts.Token
            
        member _.ClearDuplicates isIpv6 timeout =
            use cts = new CancellationTokenSource(timeout)
            if isIpv6 then
                ClearDuplicatesIpV6 cts.Token
            else
                ClearDuplicatesIpV4 cts.Token

        member _.ShowCableModem mac timeout =
            use cts = new CancellationTokenSource(timeout)
            executeCommand (sprintf "scm %s" mac) cts.Token

        member _.ShowLogging mac timeout =
            use cts = new CancellationTokenSource(timeout)
            executeCommand (sprintf "sh logging | i %s" mac) cts.Token

        member _.Dispose() = client.Dispose()
