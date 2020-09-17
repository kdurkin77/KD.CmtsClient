[<AutoOpen>]
module KD.CmtsClient.Telnet.Operators

open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Threading.Tasks

let (>>=!) (xa : unit -> Task<bool>) ca = fun() ->
    task {
        match! xa() with
        | false -> return false
        | true -> return! ca()
    }