using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V2RayG.Services
{
    public class PipeSrv :
         BaseClasses.SingletonService<PipeSrv>
    {
        const string PipeName = Apis.Models.Consts.NamedPipe.PipeName;
        const string CmdShowFormMain = Apis.Models.Consts.NamedPipe.CmdShowFormMain;
        const string CmdClosePipe = Apis.Models.Consts.NamedPipe.CmdClosePipe;

        Notifier notifier;

        PipeSrv()
        {

        }

        #region public methods
        static public void ShowFormMain()
        {
            SendCmd(CmdShowFormMain);
        }


        public void Run(Notifier notifier)
        {
            this.notifier = notifier;

            Apis.Misc.Utils.RunInBackground(() =>
            {
                var isRunning = true;
                while (isRunning)
                {
                    try
                    {
                        using (NamedPipeServerStream server = new NamedPipeServerStream(PipeName, PipeDirection.In))
                        {
                            server.WaitForConnection();
                            using (StreamReader reader = new StreamReader(server))
                            {
                                var cmd = reader.ReadLine();
                                switch (cmd)
                                {
                                    case CmdShowFormMain:
                                        this.notifier.ShowFormMain();
                                        break;
                                    case CmdClosePipe:
                                        isRunning = false;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        isRunning = false;
                    }
                }
            });
        }
        #endregion

        #region private methods
        static void SendCmd(string cmd)
        {
            try
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect();
                    using (StreamWriter writer = new StreamWriter(client))
                    {
                        writer.WriteLine(cmd);
                        writer.Flush();
                    }
                    client.WaitForPipeDrain();
                }
            }
            catch
            {

            }
        }
        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            SendCmd(CmdClosePipe);
        }

        #endregion
    }
}
