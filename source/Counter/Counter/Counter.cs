using System;
using System.Collections.Generic;
using System.Threading;

namespace Counter
{
    class Counter
    {
        private Mutex _m_cMutex;
        private Config _m_cConfig;
        private List<Data> _m_cData;

        public Counter(Mutex inMutex, Config inConfig, List<Data> inData)
        {
            this._m_cMutex = inMutex;
            this._m_cConfig = inConfig;
            this._m_cData = inData;
        }

        public void Exec()
        {
            Random cRandom = new Random();

            try
            {
                while (true)
                {
                    Data cData = new Data();
                    cData.nRandom = cRandom.Next(this._m_cConfig.nMinNumber, this._m_cConfig.nMaxNumber + 1);
                    cData.nUnixtime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    this._m_cMutex.WaitOne();
                    {
                        this._m_cData.Add(cData);
                    }
                    this._m_cMutex.ReleaseMutex();

                    Thread.Sleep(this._m_cConfig.nInterval);
                }
            }
            catch (ThreadInterruptedException)
            {
                // スレッド終了
            }
        }
    }
}
