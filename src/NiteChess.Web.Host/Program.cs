var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok", host = "web-host" }));
app.MapFallbackToFile("index.html");

app.Run();