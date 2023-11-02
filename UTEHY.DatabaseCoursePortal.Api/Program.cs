﻿using UTEHY.DatabaseCoursePortal.Api.Configurations;
using UTEHY.DatabaseCoursePortal.Api.Middlewares;
using UTEHY.DatabaseCoursePortal.Api.Providers;
using UTEHY.DatabaseCoursePortal.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppProvider();

// Provider
builder.Services.AddEntityFrameworkProvider(builder);
builder.Services.AddIdentityProvider(builder);
builder.Services.AddFluentValidationProvider();
builder.Services.AddDependencyInjectionProvider();
builder.Services.AddSwaggerProvider();
builder.Services.AddAutoMapperProvider();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
