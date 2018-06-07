using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Newtonsoft.Json;

namespace anteCompilerAPI.Controllers
{
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpPost]
        [Route("play/evaluate.json")]
        public JsonResult Post()
        {
            try
            {
                var stream = new StreamReader(this.Request.Body);
                var body = stream.ReadToEnd();
                var compilePayload = JsonConvert.DeserializeObject<CompilePayload>(body);

                var b64 = Base64Encode(compilePayload.code);

                Process process = new Process(); 
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                if (isWindows)
                {
                    process.StartInfo.FileName = "docker";
                    process.StartInfo.Arguments = "run --rm --stop-timeout 15 ante bash /home/src/run.sh " + b64;
                }
                else
                {
                    process.StartInfo.FileName = "docker";
                    process.StartInfo.Arguments = "run --rm --stop-timeout 15 ante bash -c 'bash /home/src/run.sh " + b64 + "'";
                }

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                string content = null;

                process.OutputDataReceived += (sender, args) => 
                {
                    if (args.Data != null)
                    {
                        if (content == null)
                        {
                            if (isWindows)
                            {
                                content = System.Text.RegularExpressions.Regex.Replace(args.Data, "\\u001b\\[;([0-9]*)m", "");
                            }
                            else
                            {
                                content = args.Data;
                            }
                        }
                        else
                        {
                            if (isWindows)
                            {
                                content += "\n" + System.Text.RegularExpressions.Regex.Replace(args.Data, "\\u001b\\[;([0-9]*)m" , "");
                            }
                            else
                            {
                                content += args.Data;
                            }
                        }
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit(20000);

                if (content != null)
                {
                    return new JsonResult(new CompilationResult() { conec = content });
                }
                else
                {
                    return new JsonResult(new CompilationResult() { conec = "unexpected error. Exit code is " + process.ExitCode });
                }
            }
            catch (Exception e)
            {
                return new JsonResult(new CompilationResult() { conec = "error: " + e.Message });
            }
        }

        public static string Base64Encode(string plainText) 
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }

    public class CompilationResult
    {
        public string conec;
    }

    public class CompilePayload
    {
        public string code;
        public bool color;
        public bool separate_output;
    }
}
