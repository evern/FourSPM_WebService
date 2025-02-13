using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.Extensions;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.OData.ModelBuilder;

var builder = WebApplication.CreateBuilder(args);

// Add OData services
builder.Services.AddControllers()
    .AddOData(options =>
    {

        options
            .EnableQueryFeatures()
            .SetMaxTop(null)
            .AddRouteComponents("odata/v1", EdmModelBuilder.GetEdmModel(), new DefaultODataBatchHandler());

        options
            .RouteOptions
            .EnableNonParenthesisForEmptyParameterFunction = true;
    });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
