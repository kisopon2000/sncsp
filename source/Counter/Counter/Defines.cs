using System;

namespace Counter
{
    class Defines
    {
        public const Int32 COUNTER_RTN_SUCCESS = 0;
        public const Int32 COUNTER_RTN_ERROR = 1;
        public const Int32 COUNTER_RTN_INVALID_ARG = 2;

        public const UInt32 COUNTER_ARG_MODE_NUMBER = 1;
    }

    class Config
    {
        public Int32 nMinNumber;
        public Int32 nMaxNumber;
        public Int32 nInterval;
        public Boolean bDebugMode;
        public String sDebugFilePath;
    }

    class Data
    {
        public Int32 nUnixtime;
        public Int32 nRandom;
    }
}
