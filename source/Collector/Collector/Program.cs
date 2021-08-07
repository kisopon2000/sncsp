using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Xml;

namespace Collector
{
    class Program
    {
        private Boolean _m_bActive;
        private Boolean _m_bStdout;
        private Config _m_cConfig;
        private List<NamedPipe> _m_cNamedPipes;

        Program(Boolean inStdout)
        {
            // 終了シグナル
            Console.CancelKeyPress += new ConsoleCancelEventHandler(_SignalHandler);

            // 標準出力
            this._m_bStdout = inStdout;
        }

        static void Main(string[] args)
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
            Program cProgram = new Program(cArgs.IsStdout());
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
            this._m_cNamedPipes = new List<NamedPipe>();

            // 環境変数取得
            String sConfigFilePath = Environment.GetEnvironmentVariable("KISOBE_CONFIG");

            // コンフィグ取得
            XmlDocument cXmlDoc = new XmlDocument();
            //cXmlDoc.Load(@"C:/HOME/csharp/job/environment/config/config.xml");
            cXmlDoc.Load(sConfigFilePath);
            XmlNode cNode = cXmlDoc.SelectSingleNode("config/counter_num");
            UInt32 nCounterNum = UInt32.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/min_num");
            this._m_cConfig.nMinNumber = Int32.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/max_num");
            this._m_cConfig.nMaxNumber = Int32.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/collector_interval");
            this._m_cConfig.nInterval = Int32.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/debug/enable");
            this._m_cConfig.bDebugMode = Boolean.Parse(cNode.InnerText);
            cNode = cXmlDoc.SelectSingleNode("config/debug/output");
            this._m_cConfig.sDebugFilePath = cNode.InnerText;
            //Console.WriteLine("config: {0}, {1}, {2}, {3}, {4}, {5}", nCounterNum, this._m_cConfig.nMinNumber, this._m_cConfig.nMaxNumber, this._m_cConfig.nInterval, this._m_cConfig.bDebugMode, this._m_cConfig.sDebugFilePath);

            // 名前付きパイプ
            for (UInt32 i = 0; i < nCounterNum; i++)
            {
                NamedPipe cNamedPipe = new NamedPipe();
                cNamedPipe.sName = String.Format("Counter_{0}", i + 1);
                NamedPipeClientStream cPipeClient = new NamedPipeClientStream(".", cNamedPipe.sName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
                cPipeClient.Connect();  // 接続
                cNamedPipe.cClient = cPipeClient;
                cNamedPipe.cNamedPipe = new StreamString(cPipeClient);
                this._m_cNamedPipes.Add(cNamedPipe);
            }

            // デバッグモード
            if (this._m_cConfig.bDebugMode)
            {
                if (!Directory.Exists(this._m_cConfig.sDebugFilePath))
                {
                    Directory.CreateDirectory(this._m_cConfig.sDebugFilePath);
                }
                this._m_cConfig.sDebugFilePath = System.IO.Path.Combine(this._m_cConfig.sDebugFilePath, "Collector.csv");
            }

            return Defines.COLLECTOR_RTN_SUCCESS;
        }

        private Int32 _Run()
        {
            // メインループ
            while (this._m_bActive)
            {
                Int32 nUnixtime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                String sMessage = String.Format("{0}", nUnixtime);
                String sPipeName = "";
                String sResult = "";

                try
                {
                    List<String> cCounts = new List<String>();
                    foreach (NamedPipe cNamedPipe in this._m_cNamedPipes)
                    {
                        sPipeName = cNamedPipe.sName;
                        Int32 ret = cNamedPipe.cNamedPipe.WriteString(sMessage);
                        String sCount = cNamedPipe.cNamedPipe.ReadString();
                        cCounts.Add(sCount);
                        //sCount = this._ParseCounts(sCount);
                        //Console.WriteLine("[{0}] {1}", cNamedPipe.sName, sCount);
                    }
                    if(cCounts.Count > 0)
                    {
                        sResult += "\"" + "time" + "\"" + ":";
                        sResult += nUnixtime.ToString();
                        sResult += ",";
                        sResult += this._ParseCounts(nUnixtime, cCounts);
                        if (this._m_bStdout)
                        {
                            Console.WriteLine("{" + sResult + "}");
                        }
                    }

                    // 待機
                    Thread.Sleep(this._m_cConfig.nInterval);
                }
                catch (Exception)
                {
                    if (this._m_bStdout)
                    {
                        Console.WriteLine("<!> Catch exception while interactive counter (Pipe={0})", sPipeName);
                        Console.WriteLine("Close this named pipe ...");
                    }
                    for (Int32 i = 0; i < this._m_cNamedPipes.Count; i++)
                    {
                        if(this._m_cNamedPipes[i].sName == sPipeName)
                        {
                            // 当該パイプはクローズ
                            this._m_cNamedPipes[i].cClient.Close();
                            this._m_cNamedPipes.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            return Defines.COLLECTOR_RTN_SUCCESS;
        }

        private Int32 _Postlude()
        {
            return Defines.COLLECTOR_RTN_SUCCESS;
        }

        private String _ParseCounts(Int32 inUnixtime, List<String> inCounts)
        {
            Int32 nNumber = this._m_cConfig.nMaxNumber - this._m_cConfig.nMinNumber + 1;
            UInt32[] nCounts = new UInt32[nNumber];

            foreach (String sCount in inCounts)
            {
                this._ParseCounts(sCount, nCounts);
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

            return this._ParseCounts(nCounts);
        }

        private void _ParseCounts(String inCount, UInt32[] inoutCounts)
        {
            String[] sCounts = inCount.Split(',');
            if (sCounts.Length > 0)
            {
                for (Int32 i = 0; i < sCounts.Length; i++)
                {
                    inoutCounts[i] += UInt32.Parse(sCounts[i]);
                }
            }
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
        }
    }
}
