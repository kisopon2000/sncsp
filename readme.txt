■構成
  .\source：ソースコード(C#)
     +--Counter：整数生成プロセス
     +--Collector：集計プロセス
  .\environment：実行環境
     +--config：コンフィグ
     +--system：システム

■前提
「.NET Core Runtime 3.1」を必要とする。ランタイムは以下でインストール可能。
.\environment\system\runtime\dotnet-runtime-3.1.17-win-x64.exe

■実行方法
・起動方法：.\environment\system\start.bat (集計プロセスのウィンドウが表示され、所望のJSONが確認できる)
・停止方法：.\environment\system\stop.bat

■テスト方法
.\environment\config\config.xml
 ⇒config/debug/enableをtrue
   config/debug/outputを任意の出力先にする
   ⇒起動すると、
     指定の出力先に「Counter_*.csv」「Collector.csv」が出力される。
     各整数生成プロセスそれぞれの生成結果と、集計プロセスでの合算結果を突き合わせることができる。
