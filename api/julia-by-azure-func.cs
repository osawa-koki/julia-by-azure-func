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

namespace mandelbrot
{
  public static class mandelbrot_by_azure_func
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

      // パラメータを取得する。
      if (int.TryParse(req.Query["width"], out width) == false) width = 500;
      if (int.TryParse(req.Query["height"], out height) == false) height = 500;
      if (int.TryParse(req.Query["maxIter"], out maxIter) == false) maxIter = 1000;
      if (int.TryParse(req.Query["hue"], out hue) == false) hue = 260;
      if (double.TryParse(req.Query["x0"], out x0) == false) x0 = -2.5;
      if (double.TryParse(req.Query["y0"], out y0) == false) y0 = -2.0;
      if (double.TryParse(req.Query["x1"], out x1) == false) x1 = 1.5;
      if (double.TryParse(req.Query["y1"], out y1) == false) y1 = 2.0;

      // マンデルブロ集合を描画する。
      var image = new Image<Rgba32>(width, height);

      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          double a = x0 + (x1 - x0) * x / width;
          double b = y0 + (y1 - y0) * y / height;
          double c = a;
          double d = b;
          int i = 0;
          while (i < maxIter)
          {
            double e = a * a - b * b + c;
            double f = 2 * a * b + d;
            a = e;
            b = f;
            if (a * a + b * b > 4.0) break;
            i++;
          }
          if (i == maxIter)
          {
            image[x, y] = new Rgba32(0, 0, 0);
          }
          else
          {
            // HSVからRGBへの変換
            double h = i / (double)maxIter * 360 + hue;
            double s = 1.0;
            double v = 1.0;
            double _r, _g, _b;
            if (s == 0)
            {
              _r = _g = _b = v;
            }
            else
            {
              h /= 60;
              int i1 = (int)Math.Floor(h);
              double f = h - i1;
              double p = v * (1 - s);
              double q = v * (1 - s * f);
              double t = v * (1 - s * (1 - f));
              switch (i1)
              {
                case 0:
                  _r = v;
                  _g = t;
                  _b = p;
                  break;
                case 1:
                  _r = q;
                  _g = v;
                  _b = p;
                  break;
                case 2:
                  _r = p;
                  _g = v;
                  _b = t;
                  break;
                case 3:
                  _r = p;
                  _g = q;
                  _b = v;
                  break;
                case 4:
                  _r = t;
                  _g = p;
                  _b = v;
                  break;
                default:
                  _r = v;
                  _g = p;
                  _b = q;
                  break;
              }
            }
            image[x, y] = new Rgba32((byte)(_r * 255), (byte)(_g * 255), (byte)(_b * 255));
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
