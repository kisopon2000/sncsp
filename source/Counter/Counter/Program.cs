using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Xml;

namespace Counter
{
    class Program
    {
        private String _m_sMyName;
        private Boolean _m_bActive;
        private Boolean _m_bStdout;
        private Config _m_cConfig;
        private List<Thread> _m_cThreads;
        private List<Data> _m_cData;

        Program(Boolean inStdout, String inName)
        {
            // 初期化
            this._m_sMyName = inName;

            // 終了シグナル
            Console.CancelKeyPress += new ConsoleCancelEventHandler(_SignalHandler);

            // 標準出力
            this._m_bStdout = inStdout;
        }

        static void Main(String[] args)
        {
            Int32 ret;

            // 起動パラメーター解析
            Args cArgs = new Args(args);
            if ((ret = cArgs.Parse()) != 0)
            {
                Console.WriteLine("<!> Args::Parse={0}", ret);
                Environment.Exit(ret);
            }

            // プログラム実行
            Program cProgram = new Program(cArgs.IsStdout(), cArgs.GetCounterName());
            if ((ret = cProgram._Prelude()) != 0)
            {
                Console.WriteLine("<!> Program::_Prelude()={0}", ret);
                Environment.Exit(ret);
            }
            if ((ret = cProgram._Run()) != 0)
            {
                Console.WriteLine("<!> Program::_Run()={0}", ret);
            }
            cProgram._Postlude();
        }

        private Int32 _Prelude()
        {
            // 初期化
            this._m_bActive = true;
            this._m_cConfig = new Config();
            this._m_cThreads = new List<Thread>();
            this._m_cData = new List<Data>();

            // 環境変数取得
            String sConfigFilePath = Environment.GetEnvironmentVariable("KISOBE_CONFIG");

            // コンフィグ取得
            XmlDocument cXmlDoc = new XmlDocument();
            //cXmlDoc.Load(@"C:/HOME/csharp/job/environment/config/config.xml");
            cXmlDoc.Load(sConfigFilePath);
            XmlNode cNode = cXmlDoc.SelectSingleNode("config/thread_num");
            UInt32 nThreadNum = UInt32.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/min_num");
            this._m_cConfig.nMinNumber = Int32.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/max_num");
            this._m_cConfig.nMaxNumber = Int32.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/counter_interval");
            this._m_cConfig.nInterval = Int32.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/debug/enable");
            this._m_cConfig.bDebugMode = Boolean.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/debug/output");
            this._m_cConfig.sDebugFilePath = cNode.InnerText;
            //Console.WriteLine("config: {0}, {1}, {2}, {3}, {4}, {5}", nThreadNum, this._m_cConfig.nMinNumber, this._m_cConfig.nMaxNumber, this._m_cConfig.nInterval, this._m_cConfig.bDebugMode, this._m_cConfig.sDebugFilePath);

            // スレッド起動
            Mutex cMutex = new Mutex();
            for (UInt32 i = 0; i < nThreadNum; i++)
            {
                Counter cCounter = new Counter(cMutex, this._m_cConfig, this._m_cData);
                Thread cThread = new Thread(new ThreadStart(cCounter.Exec));
                cThread.Name = String.Format("Thread_{0}", i);
                cThread.Start();
                this._m_cThreads.Add(cThread);
            }

            // デバッグモード
            if (this._m_cConfig.bDebugMode)
            {
                if (!Directory.Exists(this._m_cConfig.sDebugFilePath))
                {
                    Directory.CreateDirectory(this._m_cConfig.sDebugFilePath);
                }
                this._m_cConfig.sDebugFilePath = System.IO.Path.Combine(this._m_cConfig.sDebugFilePath, this._m_sMyName + ".csv");
            }

            return Defines.COUNTER_RTN_SUCCESS;
        }

        private Int32 _Run()
        {
            Int32 ret;
CONNECT:
            NamedPipeServerStream cNamedPipe = new NamedPipeServerStream(this._m_sMyName, PipeDirection.InOut, 1);
            cNamedPipe.WaitForConnection(); // 接続待ち
            StreamString cStreamString = new StreamString(cNamedPipe);

            // メインループ
            while (this._m_bActive)
            {
                try
                {
                    String sMessage = cStreamString.ReadString();
                    String sCount = this._GetCount(UInt32.Parse(sMessage));
                    ret = cStreamString.WriteString(sCount);
                    if (this._m_bStdout)
                    {
                        String sStdout = String.Format("\"time\":{0},{1}", sMessage, this._ParseCounts(sCount));
                        Console.WriteLine("{" + sStdout + "}");
                    }
                }
                catch (Exception)
                {
                    if (this._m_bStdout)
                    {
                        Console.WriteLine("<!> Catch exception while interactive collector");
                        Console.WriteLine("Retry connecting ...");
                    }
                    cNamedPipe.Close();
                    goto CONNECT;   // 再接続を試みる
                }
            }

            return Defines.COUNTER_RTN_SUCCESS;
        }

        private Int32 _Postlude()
        {
            // スレッド終了
            this._StopThread();

            return Defines.COUNTER_RTN_SUCCESS;
        }

        private Int32 _StopThread()
        {
            // スレッド終了
            foreach (Thread cThread in this._m_cThreads)
            {
                cThread.Interrupt();
                cThread.Join();
            }

            return Defines.COUNTER_RTN_SUCCESS;
        }

        private String _GetCount(UInt32 inUnixtime)
        {
            Int32 nNumber = this._m_cConfig.nMaxNumber - this._m_cConfig.nMinNumber + 1;
            Int32 nIndex;
            UInt32[] nCounts = new UInt32[nNumber];
            String sCount = "";

            for (Int32 i = 0; i < this._m_cData.Count; i++)
            {
                if (this._m_cData[i].nUnixtime == inUnixtime)
                {
                    nIndex = this._m_cData[i].nRandom - this._m_cConfig.nMinNumber;
                    if (nIndex >= nNumber)
                    {
                        Console.WriteLine("<!> Invalid number: {0}", this._m_cData[i].nRandom);
                        continue;
                    }
                    nCounts[nIndex] += 1;
                }
            }
            for (Int32 i = 0; i < nNumber; i++)
            {
                sCount += nCounts[i].ToString();
                sCount += ",";
            }

            if (this._m_cConfig.bDebugMode)
            {
                // デバッグ用ファイル出力
                String sDebug = "";
                sDebug += inUnixtime.ToString();
                sDebug += ",";
                foreach (UInt32 nCount in nCounts)
                {
                    sDebug += nCount;
                    sDebug += ",";
                }
                sDebug = sDebug.Substring(0, sDebug.Length - 1);
                File.AppendAllText(this._m_cConfig.sDebugFilePath, sDebug);
                File.AppendAllText(this._m_cConfig.sDebugFilePath, "\n");
            }

            return sCount.Substring(0, sCount.Length - 1);
        }

        private String _ParseCounts(UInt32[] inCounts)
        {
            String sCount = "";
            Int32 nNumber;

            if (inCounts.Length > 0)
            {
                sCount += "\"" + "counts" + "\"" + ":";
                sCount += "{";
                for (Int32 i = 0; i < inCounts.Length; i++)
                {
                    nNumber = i + this._m_cConfig.nMinNumber;
                    sCount += "\"" + nNumber.ToString() + "\"" + ":";
                    sCount += inCounts[i];
                    sCount += ",";
                }
                sCount = sCount.Substring(0, sCount.Length - 1);
                sCount += "}";

                return sCount;
            }

            return "";
        }

        private String _ParseCounts(String inCount)
        {
            String[] sCounts = inCount.Split(',');

            if (sCounts.Length > 0)
            {
                UInt32[] nCounts = new UInt32[sCounts.Length];
                for (UInt32 i = 0; i < sCounts.Length; i++)
                {
                    nCounts[i] = UInt32.Parse(sCounts[i]);
                }
                return this._ParseCounts(nCounts);
            }

            return "";
        }

        private void _SignalHandler(object sender, ConsoleCancelEventArgs e)
        {
            this._m_bActive = false;

            // スレッド終了
            this._StopThread();

            //foreach (Data cData in this._m_cData)
            //{
            //    Console.WriteLine("Counter: {0}, {1}", cData.nUnixtime, cData.nRandom);
            //}
        }
    }
}
