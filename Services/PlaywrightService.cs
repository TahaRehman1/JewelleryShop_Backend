using Microsoft.Playwright;
using System.Drawing.Printing;
using System.Threading.Tasks;

public class PlaywrightService
{
    public async Task<byte[]> GeneratePdfAsync(string html)
    {
        var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = new[]
    {
                "--no-sandbox",
        "--disable-setuid-sandbox",
        "--disable-dev-shm-usage"
    }
        });

        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);

        // Generate PDF
        var pdfBytes = await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin
            {
                Top = "20px",
                Bottom = "20px",
                Left = "20px",
                Right = "20px"
            }
        });

        await browser.CloseAsync();

        return pdfBytes;
    }
}