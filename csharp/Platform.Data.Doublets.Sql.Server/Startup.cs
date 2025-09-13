using Platform.Data.Doublets;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Sql;
using Platform.Memory;
using System.Text.Json;

namespace Platform.Data.Doublets.Sql.Server;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
            });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        services.AddSingleton<ILinks<ulong>>(sp => CreateLinks());
        services.AddSingleton<IVirtualTableManager, VirtualTableManager>();
        services.AddSingleton<ISqlParser, SqlParserImplementation>();
        services.AddSingleton<IQueryPlanner, QueryPlannerImplementation>();
        services.AddSingleton<IDoubletsQueryExecutor>(sp =>
        {
            var links = sp.GetRequiredService<ILinks<ulong>>();
            var virtualTableManager = sp.GetRequiredService<IVirtualTableManager>();
            return new DoubletsQueryExecutor<ulong>(links, virtualTableManager);
        });
        services.AddSingleton<ISqlService>(sp =>
        {
            var parser = sp.GetRequiredService<ISqlParser>();
            var planner = sp.GetRequiredService<IQueryPlanner>();
            var executor = sp.GetRequiredService<IDoubletsQueryExecutor>();
            return new SqlService<ulong>(parser, planner, executor);
        });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Doublets SQL API", Version = "v1" });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Doublets SQL API v1"));
        }

        app.UseRouting();
        app.UseCors();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Doublets SQL Server is running. Visit /swagger for API documentation.");
            });
        });
    }

    private static ILinks<ulong> CreateLinks()
    {
        var memory = new FileMappedResizableDirectMemory(Program.DatabaseFileName);
        var links = new UnitedMemoryLinks<ulong>(memory);
        return links;
    }
}