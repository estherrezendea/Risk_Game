using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Risk.Server.Services;
using System;

Console.WriteLine("=== INICIANDO SERVIDOR RISK DISTRIBUÍDO ===");

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000, o => o.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<JogoRiskService>();

app.MapGet("/", () => "O servidor gRPC do Risk está rodando. Use um cliente gRPC para se conectar.");

Console.WriteLine("Servidor rodando na porta 5000. Aguardando conexões...");

app.Run();