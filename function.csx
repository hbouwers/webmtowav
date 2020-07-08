#r "Microsoft.WindowsAzure.Storage"

using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using static Microsoft.Extensions.Logging.ILogger;

public static async Task<IActionResult> Run(HttpRequest req,
    ILogger log){

    log.LogInformation("C# HTTP trigger function processed a request.");

    var temp = Path.GetTempPath() + Path.GetRandomFileName() + ".webm";
    var tempOut = Path.GetTempPath() + Path.GetRandomFileName() + ".wav";

    log.LogInformation($"Temp In: {temp}");
    log.LogInformation($"Temp Out: {tempOut}");

    try
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        int commaStart = requestBody.IndexOf(",") + 1;          // remove "data:audio/webm;base64," and similar 
        string webmstrdata = requestBody.Substring(commaStart, requestBody.Length - commaStart);
        log.LogInformation(webmstrdata);
        byte[] webmbytesdata = Convert.FromBase64String(webmstrdata);
        File.WriteAllBytes(temp, webmbytesdata);
    }
    catch (Exception ex)
    {
        log.LogInformation(ex.Message);
        return new BadRequestObjectResult(new ProblemDetails
        {
            Status = 400,
            Title = "Exception1: " + ex.Message
        });
    }

    try
    {
        var psi = new ProcessStartInfo();
        psi.FileName = @"D:\home\site\wwwroot\ConvertAudioUsingFFMpeg\ffmpeg.exe";
        psi.Arguments = $"-i \"{temp}\" \"{tempOut}\"";
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        log.LogInformation($"Args: {psi.Arguments}");

        var process = Process.Start(psi);
        process.WaitForExit((int)TimeSpan.FromSeconds(60).TotalMilliseconds);

        string stdoutput = process.StandardOutput.ReadToEnd();
        string stderror = process.StandardError.ReadToEnd();

        log.LogInformation("FFMPEG: exitcode: " + process.ExitCode + "\r\n" + stdoutput + "\r\n" + stderror);
        log.LogInformation("FFMPEG: stdoutput: " + stdoutput);
        log.LogInformation("FFMPEG: stderror: " + stderror);

        if (process.ExitCode != 0)
        {
            return new BadRequestObjectResult(new ProblemDetails
            {
                Status = 400,
                Title = "This should be the stderror"+stderror
            });
        }
    }
    catch (Exception ex)
    {
        log.LogInformation(ex.Message);
        return new BadRequestObjectResult(new ProblemDetails
        {
            Status = 400,
            Title = "Exception2: " + ex.Message
        });
    }

    log.LogInformation("break");
    var bytes = File.ReadAllBytes(tempOut);

    File.Delete(tempOut);
    File.Delete(temp);

    await Task.Run(() => { });

    return new FileContentResult(bytes, "audio/wav");
}
