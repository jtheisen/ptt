using Ptt.Blazor.Data;
using Ptt.Blazor.Logic;

var builder = WebApplication.CreateBuilder(args);

var serivces = builder.Services;

// Add services to the container.
serivces.AddRazorPages();
serivces.AddServerSideBlazor();
serivces.AddSingleton<WeatherForecastService>();

serivces.AddScoped<InteractionManager>();
serivces.AddScoped<UiReasoningState>(sp => sp.GetRequiredService<InteractionManager>().ReasoningState);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
