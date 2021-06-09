# RAM: Returns available RAM in GB.
RAM = function() {
    kb = as.numeric(gsub("\r","",gsub("FreePhysicalMemory=","",system('wmic OS get FreePhysicalMemory /Value',intern=TRUE)[3])))
    return(kb/1024/1024)
}

# Setup: Installs and loads required libraries.
Setup = function() {
    LoadPackages = function() {
        packages = c("rlang",
                     "Rcpp",
                     "ps",
                     "processx",
                     "curl",
                     "jsonlite",
                     "backports",
                     "devtools",
                     "reticulate",
                     "tidymodels",
                     "tidyverse",
                     "timetk",
                     "ellipsis",
                     "tidyverse",
                     "modeltime.gluonts",
                     "tensorflow",
                     "keras",
                     "doParallel",
                     "RODBC",
                     "Hmisc",
                     "caret",
                     "lubridate",
                     "akmedoids",
                     "clusterSim",
                     "fastcluster",
                     "ClusterR",
                     "kohonen",
                     "RWeka",
                     "dtwclust",
                     "speccalt",
                     "Rssa",
                     "EMD",
                     "praznik",
                     "e1071",
                     "KScorrect",
                     "verification",
                     "predictionInterval",
                     "metaheuristicOpt")
        note.tf = "
=============================== IMPORTANT ===============================
Look for tensorflow-cuDNN-CUDA compatibility at https://www.tensorflow.org/install/source#gpu
In case of ModuleNotFoundError: No module named 'rpytools':
1) Try copying rpytools folder
   from <user>\\Documents\\R\\win-library\\<version>\\reticulate\\python
   to <user>\\AppData\\Local\\r-miniconda\\envs\\r-reticulate
2) Try calling
   tensorflow::install_tensorflow()
   keras::install_keras()
========================================================================="

        for (package in packages) {
            if (!require(package, character.only = TRUE, quietly = TRUE, warn.conflicts = FALSE)) {
                install.packages(package)

                if (package == "lubridate") {
                    devtools::install_github("tidyverse/lubridate")
                } else if (package == "tensorflow") {
                    write(note.tf, stdout())
                    devtools::install_github("rstudio/tensorflow", force = TRUE)
                    tensorflow::install_tensorflow(version = "2.3.0-gpu")
                } else if (package == "keras") {
                    write(note.tf, stdout())
                    devtools::install_github("rstudio/keras", force = TRUE)
                    keras::install_keras()
                } else if (package == "modeltime.gluonts") {
                        sure = menu(c("Yes", "No"), title="Are you sure you want to install GluonTS and switch to r-gluonts enviroment?")
                        if (sure == 1) {
                            Sys.setenv(R_REMOTES_NO_ERRORS_FROM_WARNINGS="true")
                            devtools::install_github("business-science/modeltime.gluonts", force = TRUE)
                            Sys.setenv(R_REMOTES_NO_ERRORS_FROM_WARNINGS="false")
                            
                            py = readline("Please enter your Python version:")
                            cuda = readline("Please enter your CUDA version:")
                            mx = paste("mxnet-cu", gsub("[.]", "", cuda), sep="")
                            install.packages("https://s3.ca-central-1.amazonaws.com/jeremiedb/share/mxnet/GPU/mxnet.zip", repos = NULL)
                            reticulate::py_install(
                                envname  = "r-gluonts",
                                python_version = py,
                                packages = c(mx, "gluonts", "pandas", "numpy", "pathlib"),
                                method = "conda",
                                pip = TRUE
                            )

                            modeltime.gluonts::install_gluonts() # Microsoft Visual C++ 14.0 is required. Get it with "Build Tools for Visual Studio": https://visualstudio.microsoft.com/downloads/
                            modeltime.gluonts::install_gluonts() # Avoiding "Error in pkg.env$gluonts$distribution$student_t$StudentTOutput() : attempt to apply non-function".
                        }
                }

                library(package, character.only = TRUE, quietly = TRUE, warn.conflicts = FALSE)
            }
        }
    }

    # Suppressing warnings.
    options(warn = -1)

    # Changing default 256MB JVM memory limit before loading Java packages:
    options(java.parameters = paste("-Xmx", round(RAM() * 0.8), "g", sep = ""))

    # Avoding the need for user inputs:
    options(install.packages.compile.from.source = "always")

    # Loading required libraries without warnings:
    suppressWarnings(suppressMessages({ LoadPackages() }))

    # Disabling TensorFlow info messages:
    py_run_string("import os; os.environ['TF_CPP_MIN_LOG_LEVEL'] = '2';")
}

Setup()