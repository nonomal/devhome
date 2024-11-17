﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevHome.Common.TelemetryEvents.DevHomeDatabase;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Factories;
using DevHome.Telemetry;
using Serilog;

namespace DevHome.Database.Services;

public class RepositoryManagementDataAccessService
{
    private const string EventName = "DevHome_RepositoryData_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementDataAccessService));

    private readonly DevHomeDatabaseContextFactory _databaseContextFactory;

    public RepositoryManagementDataAccessService(
        DevHomeDatabaseContextFactory databaseContextFactory)
    {
        _databaseContextFactory = databaseContextFactory;
    }

    /// <summary>
    /// Makes a new Repository entity with the provided name and location then saves it
    /// to the database.
    /// </summary>
    /// <returns>The new repository.  Can return null if the database threw an exception.</returns>
    public Repository MakeRepository(string repositoryName, string cloneLocation, string repositoryUri)
    {
        return MakeRepository(repositoryName, cloneLocation, string.Empty, repositoryUri);
    }

    public Repository MakeRepository(string repositoryName, string cloneLocation, string configurationFileLocationAndName, string repositoryUri)
    {
        return MakeRepository(repositoryName, cloneLocation, configurationFileLocationAndName, repositoryUri, null);
    }

    public Repository MakeRepository(string repositoryName, string cloneLocation, string configurationFileLocationAndName, string repositoryUri, Guid? sourceControlProviderClassId)
    {
        var existingRepository = GetRepository(repositoryName, cloneLocation);
        if (existingRepository != null)
        {
            _log.Information($"A Repository with name {repositoryName} and clone location {cloneLocation} exists in the repository already.");
            return existingRepository;
        }

        Repository newRepo = new()
        {
            RepositoryName = repositoryName,
            RepositoryClonePath = cloneLocation,
            RepositoryUri = repositoryUri,
            SourceControlClassId = sourceControlProviderClassId,
        };

        if (!string.IsNullOrEmpty(configurationFileLocationAndName))
        {
            if (!File.Exists(configurationFileLocationAndName))
            {
                _log.Information($"No file exists at {configurationFileLocationAndName}.  This repository will not have a configuration file.");
            }
            else
            {
                newRepo.ConfigurationFileLocation = configurationFileLocationAndName;
            }
        }

        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            dbContext.Add(newRepo);
            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Exception when saving in {nameof(MakeRepository)}");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DevHomeDatabaseEvent(nameof(MakeRepository), ex));
            return new Repository();
        }

        return newRepo;
    }

    public List<Repository> GetRepositories()
    {
        _log.Information("Getting repositories");
        List<Repository> repositories = [];

        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            repositories = [.. dbContext.Repositories];
        }
        catch (Exception ex)
        {
            _log.Error(ex, ex.ToString());
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(GetRepositories), ex));
        }

        return repositories;
    }

    public Repository? GetRepository(string repositoryName, string cloneLocation)
    {
        _log.Information("Getting a repository");
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
#pragma warning disable CA1309 // Use ordinal string comparison
            return dbContext.Repositories.FirstOrDefault(x => x.RepositoryName!.Equals(repositoryName)
            && string.Equals(x.RepositoryClonePath, Path.GetFullPath(cloneLocation)));
#pragma warning restore CA1309 // Use ordinal string comparison
        }
        catch (Exception ex)
        {
            _log.Error(ex, ex.ToString());
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(GetRepository), ex));
        }

        return null;
    }

    public bool UpdateCloneLocation(Repository repository, string newLocation)
    {
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            var repositoryToUpdate = dbContext.Repositories.Find(repository.RepositoryId);
            if (repositoryToUpdate == null)
            {
                _log.Warning($"{nameof(UpdateCloneLocation)} was called with a RepositoryId of {repository.RepositoryId} and it does not exist in the database.");
                return false;
            }

            // Maybe update the tracking information on repository.  This way
            // EF will catch the change.
            repository.RepositoryClonePath = newLocation;
            repositoryToUpdate.RepositoryClonePath = newLocation;

            if (repository.HasAConfigurationFile)
            {
                var configurationFolder = Path.GetDirectoryName(repository.ConfigurationFileLocation);
                var configurationFileName = Path.GetFileName(configurationFolder);

                repository.ConfigurationFileLocation = Path.Combine(newLocation, configurationFolder ?? string.Empty, configurationFileName ?? string.Empty);
                repositoryToUpdate.ConfigurationFileLocation = Path.Combine(newLocation, configurationFolder ?? string.Empty, configurationFileName ?? string.Empty);
            }

            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when updating the clone location.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(UpdateCloneLocation), ex));
            return false;
        }

        return true;
    }

    public bool SetSourceControlId(Repository repository, Guid sourceControlId)
    {
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            var repositoryToUpdate = dbContext.Repositories.Find(repository.RepositoryId);
            if (repositoryToUpdate == null)
            {
                _log.Warning($"{nameof(UpdateCloneLocation)} was called with a RepositoryId of {repository.RepositoryId} and it does not exist in the database.");
                return false;
            }

            // TODO: Figure out a method to update the entity in the database and
            // the entity in memory.
            repository.SourceControlClassId = sourceControlId;
            repositoryToUpdate.SourceControlClassId = sourceControlId;

            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when updating the clone location.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(UpdateCloneLocation), ex));
            return false;
        }

        return true;
    }

    public void SetIsHidden(Repository repository, bool isHidden)
    {
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            var repositoryToUpdate = dbContext.Repositories.Find(repository.RepositoryId);
            if (repositoryToUpdate == null)
            {
                _log.Warning($"{nameof(SetIsHidden)} was called with a RepositoryId of {repository.RepositoryId} and it does not exist in the database.");
                return;
            }

            repositoryToUpdate.IsHidden = isHidden;
            repository.IsHidden = isHidden;

            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when setting repository hidden status.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(UpdateCloneLocation), ex));
            return;
        }
    }

    public void RemoveRepository(Repository repository)
    {
        try
        {
            using var dbContext = _databaseContextFactory.GetNewContext();
            var repositoryToRemove = dbContext.Repositories.Find(repository.RepositoryId);
            if (repositoryToRemove == null)
            {
                _log.Warning($"{nameof(RemoveRepository)} was called with a RepositoryId of {repository.RepositoryId} and it does not exist in the database.");
                return;
            }

            dbContext.Repositories.Remove(repositoryToRemove);
            dbContext.SaveChanges();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Exception when removing the repository.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_Database_Event",
                LogLevel.Critical,
                new DatabaseEvent(nameof(UpdateCloneLocation), ex));
            return;
        }
    }
}
