﻿using System;
using System.Collections.Generic;
using System.IO;
using Calamari.Integration.Processes;

namespace Calamari.Deployment.Features.Java
{
    /// <summary>
    /// A base class for features that run Java against the Octopus Deploy 
    /// Java library
    /// </summary>
    public abstract class JavaBaseFeature
    {
        
        private readonly ICommandLineRunner commandLineRunner;
        
        protected JavaBaseFeature(ICommandLineRunner commandLineRunner)
        {
            this.commandLineRunner = commandLineRunner;
        }
        
        /// <summary>
        /// Execute java running the Octopus Deploy Java library
        /// </summary>
        protected void runJava(string mainClass, Dictionary<string,string> environmentVariables)
        {           
            /*
                The precondition script will set the OctopusEnvironment_Java_Bin environment variable based
                on where it found the java executable based on the JAVA_HOME environment
                variable. If OctopusEnvironment_Java_Bin is empty or null, it means that the precondition
                found java on the path.
            */
            var javaBin = Environment.GetEnvironmentVariable(SpecialVariables.Action.Java.JavaBinEnvVar) ?? "";
            /*
                The precondition script will also set the location of the calamari.jar file
            */
            var javaLib = Environment.GetEnvironmentVariable(SpecialVariables.Action.Java.JavaLibraryEnvVar) ?? "";
            var result = commandLineRunner.Execute(new CommandLineInvocation(
                Path.Combine(javaBin, "java"), 
                $"-cp calamari.jar {mainClass}",
                Path.Combine(javaLib, "contentFiles", "any", "any"),
                environmentVariables));
            
            result.VerifySuccess();
        }
    }
}