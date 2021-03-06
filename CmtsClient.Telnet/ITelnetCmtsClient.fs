﻿namespace KD.CmtsClient.Telnet

open System
open System.Threading.Tasks

type ITelnetCmtsClient =
    inherit IDisposable
    abstract ConnectAsync : unit -> Task
    abstract IsConnected : unit -> bool
    abstract SendKeepAlive : TimeSpan -> Task
    abstract Login : string -> string -> string -> TimeSpan -> Task<bool>
    abstract ClearDuplicates : bool -> TimeSpan -> Task<bool>
    abstract ShowCableModem : string -> TimeSpan -> Task<string>
    abstract ShowLogging : string -> TimeSpan -> Task<string>
    abstract ClearDuplicatesIpV4: TimeSpan -> Task<bool>
    abstract ClearDuplicatesIpV6: TimeSpan -> Task<bool>