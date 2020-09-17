[<AutoOpen>]
module KD.CmtsClient.Telnet.Extensions

open System.Threading.Tasks

type Task with
    static member Ignore (x: Task<_>) = x :> Task