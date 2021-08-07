using System;

namespace Collector
{
    class Args
    {
        private String[] _m_sArgs;
        private Boolean _m_bStdout;

        public Args(String[] inArgs)
        {
            this._m_sArgs = inArgs;
            this._m_bStdout = false;
        }

        public Int32 Parse()
        {
            UInt32 nMode = 0;
            foreach (String sArg in this._m_sArgs)
            {
                if ((nMode == 0) && (sArg[0] == '/'))
                {
                    if (false) { }
                    else if (sArg == "/Stdout") { this._m_bStdout = true; }
                    else
                    {
                        Console.WriteLine("<!> Not support arg: {0}", sArg);
                        return Defines.COLLECTOR_RTN_INVALID_ARG;
                    }
                    continue;
                }

                switch (nMode)
                {
                    default:
                        Console.WriteLine("<!> Not support mode: {0}", nMode);
                        break;
                }
            }

            return Defines.COLLECTOR_RTN_SUCCESS;
        }

        public Boolean IsStdout()
        {
            return this._m_bStdout;
        }
    }
}
