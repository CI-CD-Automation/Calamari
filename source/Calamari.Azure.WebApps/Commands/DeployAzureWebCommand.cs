﻿using System.Collections.Generic;
using System.IO;
using Calamari.Azure.WebApps.Deployment.Conventions;
using Calamari.Commands;
using Calamari.Commands.Support;
using Calamari.Deployment;
using Calamari.Deployment.Conventions;
using Calamari.Integration.ConfigurationTransforms;
using Calamari.Integration.ConfigurationVariables;
using Calamari.Integration.FileSystem;
using Calamari.Integration.JsonVariables;
using Calamari.Integration.Packages;
using Calamari.Integration.Processes;
using Calamari.Integration.Scripting;
using Calamari.Integration.ServiceMessages;
using Calamari.Integration.Substitutions;

namespace Calamari.Azure.WebApps.Commands
{
    [Command("deploy-azure-web", Description = "Extracts and installs a deployment package to an Azure Web Application")]
    public class DeployAzureWebCommand : Command
    {
        private string packageFile;
        private readonly IScriptEngine scriptEngine;
        readonly IVariables variables;
        readonly ICommandLineRunner commandLineRunner;
        readonly IConventionFactory conventionFactory;

        public DeployAzureWebCommand(IScriptEngine scriptEngine, IVariables variables, ICommandLineRunner commandLineRunner, IConventionFactory conventionFactory)
        {
            Options.Add("package=", "Path to the deployment package to install.", v => packageFile = Path.GetFullPath(v));

            this.scriptEngine = scriptEngine;
            this.variables = variables;
            this.commandLineRunner = commandLineRunner;
            this.conventionFactory = conventionFactory;
        }

        public override int Execute(string[] commandLineArguments)
        {
            Options.Parse(commandLineArguments);

            Guard.NotNullOrWhiteSpace(packageFile,
                "No package file was specified. Please pass --package YourPackage.nupkg");

            if (!File.Exists(packageFile))
                throw new CommandException("Could not find package file: " + packageFile);

            Log.Info("Deploying package:    " + packageFile);

            var fileSystem = new WindowsPhysicalFileSystem();
            var replacer = new ConfigurationVariablesReplacer(variables.GetFlag(SpecialVariables.Package.IgnoreVariableReplacementErrors));
            var jsonReplacer = new JsonConfigurationVariableReplacer();
            var configurationTransformer = ConfigurationTransformer.FromVariables(variables);
            var transformFileLocator = new TransformFileLocator(fileSystem);

            var conventions = new List<IConvention>
            {
                new ExtractPackageToStagingDirectoryConvention(new GenericPackageExtractorFactory().createStandardGenericPackageExtractor(), fileSystem),
                new ConfiguredScriptConvention(DeploymentStages.PreDeploy, fileSystem, scriptEngine, commandLineRunner),
                new PackagedScriptConvention(DeploymentStages.PreDeploy, fileSystem, scriptEngine, commandLineRunner),
                conventionFactory.SubstituteInFilesBasedOnVariableValues(),
                new ConfigurationTransformsConvention(fileSystem, configurationTransformer, transformFileLocator),
                new ConfigurationVariablesConvention(fileSystem, replacer),
                new JsonConfigurationVariablesConvention(jsonReplacer, fileSystem),
                new PackagedScriptConvention(DeploymentStages.Deploy, fileSystem, scriptEngine, commandLineRunner),
                new ConfiguredScriptConvention(DeploymentStages.Deploy, fileSystem, scriptEngine, commandLineRunner),
                new AzureWebAppConvention(),
                new LogAzureWebAppDetails(),
                new PackagedScriptConvention(DeploymentStages.PostDeploy, fileSystem, scriptEngine, commandLineRunner),
                new ConfiguredScriptConvention(DeploymentStages.PostDeploy, fileSystem, scriptEngine, commandLineRunner),
            };

            var deployment = new RunningDeployment(packageFile, variables);
            var conventionRunner = new ConventionProcessor(deployment, conventions);

            conventionRunner.RunConventions();

            return 0;
        }
    }
}