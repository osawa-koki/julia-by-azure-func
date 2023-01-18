using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace julia
{
  public static class julia_by_azure_func
  {
    [FunctionName("julia_by_azure_func")]
    public static async Task<IActionResult> Run(
      [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
      ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");

      string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
      dynamic data = JsonConvert.DeserializeObject(requestBody);

      int width;
      int height;
      int maxIter;
      int hue;
      double x0;
      double y0;
      double x1;
      double y1;
      double cx;
      double cy;

      // パラメータを取得する。
      if (int.TryParse(req.Query["width"], out width) == false) width = 500;
      if (int.TryParse(req.Query["height"], out height) == false) height = 500;
      if (int.TryParse(req.Query["maxIter"], out maxIter) == false) maxIter = 100;
      if (int.TryParse(req.Query["hue"], out hue) == false) hue = 260;
      if (double.TryParse(req.Query["x0"], out x0) == false) x0 = -2.0;
      if (double.TryParse(req.Query["y0"], out y0) == false) y0 = -2.0;
      if (double.TryParse(req.Query["x1"], out x1) == false) x1 = 2.0;
      if (double.TryParse(req.Query["y1"], out y1) == false) y1 = 2.0;
      if (double.TryParse(req.Query["cx"], out cx) == false) cx = -0.8;
      if (double.TryParse(req.Query["cy"], out cy) == false) cy = 0.156;

      // ジュリア集合を計算する。
      var image = new Image<Rgba32>(width, height);

      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          double zx = x0 + (x1 - x0) * x / width;
          double zy = y0 + (y1 - y0) * y / height;
          int iter = 0;

          while (zx * zx + zy * zy < 4 && iter < maxIter)
          {
            double tmp = zx * zx - zy * zy + cx;
            zy = 2 * zx * zy + cy;
            zx = tmp;
            iter++;
          }

          if (iter < maxIter)
          {
            double t = (double)iter / maxIter;
            double r = Math.Sin(0.5 * Math.PI * t + 0.0 * Math.PI) * 255;
            double g = Math.Sin(0.5 * Math.PI * t + 1.0 * Math.PI) * 255;
            double b = Math.Sin(0.5 * Math.PI * t + 2.0 * Math.PI) * 255;
            image[x, y] = new Rgba32((byte)r, (byte)g, (byte)b);
          }
          else
          {
            image[x, y] = new Rgba32(0, 0, 0);
          }
        }
      }

      // レスポンスを返す。
      var responseMessage = new MemoryStream();
      image.SaveAsPng(responseMessage);
      responseMessage.Position = 0;

      // Content-Typeを指定する。
      req.HttpContext.Response.ContentType = "image/png";

      return new OkObjectResult(responseMessage);
    }
  }
}
