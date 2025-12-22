namespace KD.CmtsClient.Ssh

open System
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
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            use cts = new CancellationTokenSource(timeout)
            executeCommand String.Empty cts.Token :> Task

        member _.ConnectAndLogin username password _ timeout =
            if String.IsNullOrWhiteSpace username then
                raise (ArgumentNullException(nameof username))
            if String.IsNullOrWhiteSpace password then
                raise (ArgumentNullException(nameof password))
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            task {
                client <- new SshClient(ip.ToString(), username, password)
                use cts = new CancellationTokenSource(timeout)
                do! client.ConnectAsync(cts.Token)
                return true
            }

        member _.ClearDuplicatesIpV4 timeout =
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            use cts = new CancellationTokenSource(timeout)
            ClearDuplicatesIpV4 cts.Token

        member _.ClearDuplicatesIpV6 timeout =
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            use cts = new CancellationTokenSource(timeout)
            ClearDuplicatesIpV6 cts.Token

        member _.ClearDuplicates isIpv6 timeout =
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            use cts = new CancellationTokenSource(timeout)
            if isIpv6 then
                ClearDuplicatesIpV6 cts.Token
            else
                ClearDuplicatesIpV4 cts.Token

        //arg could be a mac to get specifically the info for that mac or empty to get all the ipv4 info or ipv6 to see all the ipv6 info
        member _.ShowCableModem arg timeout =
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            use cts = new CancellationTokenSource(timeout)
            let command =
                if String.IsNullOrWhiteSpace arg then
                    "scm"
                else
                    $"scm {arg}"
            executeCommand command cts.Token

        member _.ShowLogging mac timeout =
            if String.IsNullOrWhiteSpace mac then
                raise (ArgumentNullException(nameof mac))
            if timeout < TimeSpan.Zero then
                raise (ArgumentOutOfRangeException(nameof(timeout), $"{nameof timeout} must be greater than or equal to 0"))

            use cts = new CancellationTokenSource(timeout)
            executeCommand ($"sh logging | i {mac}") cts.Token

        member _.Dispose() = client.Dispose()
