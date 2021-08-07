using System;

namespace Counter
{
    class Args
    {
        private String[] _m_sArgs;
        private Boolean _m_bStdout;
        private Int32 _m_nNumber;

        public Args(String[] inArgs)
        {
            this._m_sArgs = inArgs;
            this._m_bStdout = false;
            this._m_nNumber = -1;
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
                    else if (sArg == "/Number") { nMode = Defines.COUNTER_ARG_MODE_NUMBER; }
                    else
                    {
                        Console.WriteLine("<!> Not support arg: {0}", sArg);
                        return Defines.COUNTER_RTN_INVALID_ARG;
                    }
                    continue;
                }

                switch (nMode)
                {
                    case Defines.COUNTER_ARG_MODE_NUMBER: { this._m_nNumber = Int32.Parse(sArg); } break;
                    default:
                        Console.WriteLine("<!> Not support mode: {0}", nMode);
                        break;
                }
            }

            return Defines.COUNTER_RTN_SUCCESS;
        }

        public Boolean IsStdout()
        {
            return this._m_bStdout;
        }

        public String GetCounterName()
        {
            if(this._m_nNumber == -1)
            {
                return "Counter";
            }
            else
            {
                return String.Format("Counter_{0}", this._m_nNumber);
            }
        }
    }
}
