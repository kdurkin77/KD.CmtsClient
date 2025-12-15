namespace KD.CmtsClient

open System
open System.Threading.Tasks

type ICmtsClient =
    inherit IDisposable
    abstract ConnectAndLogin     : string   -> string     -> string        -> TimeSpan -> Task<bool>
    abstract IsConnected         : unit     -> bool
    abstract SendKeepAlive       : TimeSpan -> Task
    abstract ClearDuplicates     : bool     -> TimeSpan   -> Task<bool>
    abstract ShowCableModem      : string   -> TimeSpan   -> Task<string>
    abstract ShowLogging         : string   -> TimeSpan   -> Task<string>
    abstract ClearDuplicatesIpV4 : TimeSpan -> Task<bool>
    abstract ClearDuplicatesIpV6 : TimeSpan -> Task<bool>
