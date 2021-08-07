using System;
using System.IO.Pipes;

namespace Collector
{
    class Defines
    {
        public const Int32 COLLECTOR_RTN_SUCCESS = 0;
        public const Int32 COLLECTOR_RTN_ERROR = 1;
        public const Int32 COLLECTOR_RTN_INVALID_ARG = 2;
    }

    class Config
    {
        public Int32 nMinNumber;
        public Int32 nMaxNumber;
        public Int32 nInterval;
        public Boolean bDebugMode;
        public String sDebugFilePath;
    }

    class NamedPipe
    {
        public String sName;
        public StreamString cNamedPipe;
        public NamedPipeClientStream cClient;
    }
}
