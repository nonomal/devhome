﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq.Expressions;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Serilog;

namespace DevHome.Database;

/// <summary>
/// To make the database please run the following in Package Manager Console
/// Update-Database -StartupProject DevHome.Database -Project DevHome.Database
///
/// TODO: Remove this comment after database migration is implemeneted.
/// TODO: Set up Github detection for files in this project.
/// TODO: Add documentation around migration and Entity Framework in DevHome.
/// </summary>
public class DevHomeDatabaseContext : DbContext
{
    private const string DatabaseFileName = "DevHome.db";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevHomeDatabaseContext));

    public DbSet<Repository> Repositories { get; set; }

    public string DbPath { get; }

    public DevHomeDatabaseContext()
    {
        // TODO: How to run the DevHome in VS and not have the file move to the per app location.
        DbPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFileName);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO: Use ServiceExtensions as an example to set up individual
        // models using fluent API.  Currently, not needed, but will as this method
        // will expand as more entities are added.
        // If that is too much work these definitions can be placed inside the C# class.
        try
        {
            // TODO: How to update "UpdatedAt"?
            var repositoryEntity = modelBuilder.Entity<Repository>();
            if (repositoryEntity != null)
            {
                repositoryEntity.Property(x => x.ConfigurationFileLocation).HasDefaultValue(string.Empty);
                repositoryEntity.Property(x => x.RepositoryClonePath).HasDefaultValue(string.Empty).IsRequired(true);
                repositoryEntity.Property(x => x.RepositoryName).HasDefaultValue(string.Empty).IsRequired(true);
                repositoryEntity.Property(x => x.CreatedUTCDate).HasDefaultValueSql("datetime()");
                repositoryEntity.Property(x => x.UpdatedUTCDate).HasDefaultValueSql("datetime()");
                repositoryEntity.Property(x => x.RepositoryUri).HasDefaultValue(string.Empty);
                repositoryEntity.Property(x => x.SourceControlClassId).HasDefaultValue(Guid.Empty);
                repositoryEntity.ToTable("Repository");
            }
        }
        catch (Exception ex)
        {
            // TODO: Notify user the database could not initialize.
            _log.Error(ex, "Can not build the database model");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_DatabaseContext_Event",
                LogLevel.Critical,
                new DatabaseContextErrorEvent("CreatingModel", ex));
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }
}
