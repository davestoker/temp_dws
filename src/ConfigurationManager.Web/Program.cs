using ConfigurationManager.Web.Components;
using ConfigurationManager.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<CurrentSession>();
builder.Services.AddSingleton<DemoApiKeyDirectory>();
builder.Services.AddTransient<ApiKeyHandler>();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:7071/api/";
builder.Services.AddHttpClient<ConfigurationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.EndsWith('/') ? apiBaseUrl : apiBaseUrl + "/");
}).AddHttpMessageHandler<ApiKeyHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
