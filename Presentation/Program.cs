using Azure.Communication.Email;
using Presentation.Interfaces;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(x => new EmailClient(builder.Configuration["ACS:ConnectionString"]));
builder.Services.AddTransient<IVerificationService, VerificationService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
}
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();

app.MapControllers();

app.Run();