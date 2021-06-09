# ForecastSet: Prepares dataset for time series forecast testing.
#' @param data: X and Y values (one column per variable and one row per example).
#' @param col_time: Timestamp column index.
#' @param cols_x_required: Required X column indexes.
#' @param cols_x_optional: Optional X column indexes.
#' @param cols_y: Y column indexes.
#' @param horizon: The length of forecast horizon.
#' @param lookback: The length of Y past to take as input.
#' @param splitter: Row that splits training from test set (last training set row).
#' @param uncertainty: The number of rows to shift back and forth while matching optional X with Y values
#' @param encoding: One of available time encoding types:
#' 1) "polar": Polar coordinate system.
#' 2) "onehot": One-Hot encoding.
#' @param x.normalization: One of available normalization types for X values:
#' 1) "NA": Not Assigned (none).
#' 2) "AM": Abs-Max normalization
#' 3) "MM": Min-Max normalization
#' 4) "AVG": Divide-by-average normalization
#' 5) "Z": Z-normalization
#' @param y.normalization: One of available normalization types for Y values:
#' 1) "NA": Not Assigned (none).
#' 2) "AM": Abs-Max normalization
#' 3) "MM": Min-Max normalization
#' 4) "AVG": Divide-by-average normalization
#' 5) "Z": Z-normalization
#' @return A list with two members:
#' 1) train: Training data .
#' 2) test: Test data split by horizon.
#' 3) norm: Normalization parameters.
ForecastSet = function(data,
                       col_time,
                       cols_x_required,
                       cols_x_optional,
                       cols_y,
                       horizon,
                       lookback,
                       splitter,
                       uncertainty = 0,
                       encoding = 'polar',
                       x.normalization = 'MM',
                       y.normalization = 'MM') {
    PrepareSet = function() {
        Get = function(cols) return(Subset(data, cols = cols))
        Time = function() return(EncodeTime(DateTime(data[,col_time]),type=encoding))
        RequiredX = function() return(Get(cols_x_required))
        OptionalX = function() {
            p = Get(cols_x_optional)
            slider = -uncertainty:uncertainty
            candidates = as.data.frame(Slide(p, slider, FALSE))
            colnames(candidates) = Combine(colnames(p), slider)
            return(candidates)
        }
        SelectX = function(x, y, focus) {
            selection = c()
            for(i in 1:ncol(y)) selection = c(selection, colnames(SelectFeatures(x, y[,i], focus)))
            return(Subset(x, cols = unique(selection)))
        }

        t = Get(col_time)
        y = Get(cols_y)
        x.base = Join(Time(), RequiredX())
        x.opt = SelectX(OptionalX(), y, TrainRows())
        x = Join(x.base, x.opt)
        return(list(t=t, x=x, y=y))
    }
    NormParams = function(x, y) {
        nx = NormParam(x, x.normalization, 'col')
        ny = NormParam(y, y.normalization, 'col')
        return(list(x=nx, y=ny))
    }
    TrainRows = function() return(1:splitter)
    TrainSubset = function(t, x, y) {
        rows = TrainRows()
        t = Subset(t, rows=rows)
        x = Subset(x, rows=rows)
        y = Subset(y, rows=rows)
        norm = NormParams(x, y)
        x = Norm(x, norm$x)
        y = Norm(y, norm$y)
        set = list(t=t, x=x, y=y)
        return(list(set=set, norm=norm))
    }
    TestRows = function(splitter) return((splitter - lookback + 1):(splitter + horizon))
    TestSubsets = function(t, x, y, norm) {
        TestSubset = function(rows) {
            t = Subset(t, rows=rows)
            x = Norm(Subset(x, rows=rows), norm$x)
            y = Norm(Subset(y, rows=rows), norm$y)
            return(list(t=t, x=x, y=y))
        }

        test = list()
        for(i in 1:floor((Count(y)-splitter-uncertainty)/horizon)) {
            test[[i]] = TestSubset(TestRows(splitter+(i-1)*horizon))
        }

        return(test)
    }

    set = PrepareSet()
    train = TrainSubset(set$t, set$x, set$y)
    test = TestSubsets(set$t, set$x, set$y, train$norm)
    return(list(train = train$set, test = test, norm = train$norm))
}

#' ForecastEval: Runs time series forecast evaluation.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @param sd: Expected standard deviations.
#' @param n: Sample size for probabilistic forecast.
#' @param q: Number of quantiles for probabilistic forecast.
#' @param p: Probability levels for probabilistic forecast.
#' @param verbose: Shows whether or not to plot forecast results.
#' @param x_name: Name of x-axis.
#' @param y_name: Name of y-axis.
#' @param x_min: Min x-axis value.
#' @param x_max: Max x-axis value.
#' @param y_min: Min y-axis value.
#' @param y_max: Max y-axis value.
#' @param width: Line width.
#' @return Vector of forecast errors:
#' 1) "MAE": Mean Absolute Error for deterministic forecast.
#' 2) "MAPE": Mean Absolute Percentage Error for deterministic forecast.
#' 3) "RMSE": Root Mean Square Error for deterministic forecast.
#' 4) "CV": Coefficient of Variation of the Root Mean Square Error for deterministic forecast.
#' 5) "CRPS": Continuous Ranked Probability Score for probabilistic forecast.
#' 6) "QS": Quantile Score for probabilistic forecast.
#' 7) "ACE": Average Coverage Error for probabilistic forecast.
#' 8) "WS": Winkler Score for probabilistic forecast.
ForecastEval = function(y_true,
                        y_pred,
                        sd = c(),
                        n = 1,
                        q = 100,
                        p = c(0.90,0.95,0.99),
                        verbose = FALSE,
                        x_name = 'time',
                        y_name = 'value',
                        x_min = 1,
                        x_max = Count(unlist(y_true)),
                        y_min = min(y_pred-sd),
                        y_max = max(y_pred+sd),
                        width = 1) {
    Plot = function(lines, areas, title, colors) {
        X = function() return(x_min:x_max)
        Y = function() return(c(y_min, y_max))
        MakePlot = function() {
            plot(x = X(), ylim = Y(), type = "l", main = title, xlab = x_name, ylab = y_name)
        }
        AddLegend = function() {
            legend("top", horiz = TRUE, legend = names(colors), col = colors, lty = 1, lwd = 3*width, cex = 0.9)
        }
        AddLines = function() {
            if (Count(lines) == 0) return(c())
            for(i in 1:ncol(lines)) {
                color = colors[names(lines)[i]]
                lines(X(), lines[,i], type = "l", col = color, lwd = width)
            }
        }
        AddAreas = function() {
            if (Count(areas) == 0) return(c())
            x_values = c(X(), rev(X()))
            for(i in 1:ncol(areas)) {
                color = colors[names(areas)[i]]
                polygon(x_values, areas[,i], col = color, border = NA)
            }
        }

        MakePlot()
        AddAreas()
        AddLines()
        AddLegend()
    }

    y_true = as.vector(unlist(y_true))
    y_pred = as.vector(unlist(y_pred))
    sd = as.vector(unlist(sd))

    errors = c("MAE"  = MAE(y_true, y_pred),
               "MAPE" = MAPE(y_true, y_pred),
               "RMSE" = RMSE(y_true, y_pred),
               "CV"   = CVRMSE(y_true, y_pred))
    if (Count(sd) > 0) {
        e = c("CRPS" = CRPS(y_true, y_pred, sd),
              "QS"   = QS(y_true, y_pred, sd, n, q))
        errors = c(errors, e)
        for (prob in p) {
            e = c("ACE" = ACE(y_true, y_pred, sd, n, prob),
                  "WS"  =  WS(y_true, y_pred, sd, n, prob))
            errors = c(errors, e)
            perc = paste(c("(", prob * 100, "%", ")"), collapse = "")
            last = Count(errors)-1:Count(e)+1
            names(errors)[last] = paste(names(errors)[last], perc, sep = "")
        }
    }

    if (verbose) {
        lines = data.frame()
        areas = data.frame()

        colors = c("actual" = "black", "expected" = "darkgreen")
        lines = Join(lines, y_pred, colnames2 = names(colors)[2])
        lines = Join(lines, y_true, colnames2 = names(colors)[1])
        if (Count(sd) > 0) {
            colors = c(colors, "green")
            names(colors)[Count(colors)] = "SD"
            areas = Join(areas, c(y_pred-sd, rev(y_pred+sd)), colnames2 = "SD")
        }

        Plot(lines, areas, Num2Str(errors), colors)
    }

    return(errors)
}

#' ForecastTest: Runs time series forecast testing.
#' Trains selected forecast model with data before the splitter and
#' tests the model iteratively for each horizon after the splitter.
#' @param type: One of available types of forecast models:
#' 1) "SVR": Support Vector Regression (SVR)
#' 2) "DCL": regression model for Deep Centroid Learning (DCL)
#' 3) "DEL": regression model for Deep Ensemble Learning (DEL)
#' 4) "DMN": Deep Mixture Network (DMN)
#' 5) "DeepAR": Deep Auto-Regression (DeepAR)
#' @param data: X and Y values (one column per variable and one row per example).
#' @param col_time: Timestamp column index.
#' @param cols_x_required: Required X column indexes.
#' @param cols_x_optional: Optional X column indexes.
#' @param cols_y: Y column indexes.
#' @param horizon: The length of forecast horizon.
#' @param lookback: The length of Y past to take as input.
#' @param splitter: Row that splits training from test set (last training set row).
#' @param uncertainty: The number of rows to shift back and forth while matching optional X with Y values
#' @param encoding: One of available time encoding types:
#' 1) "polar": Polar coordinate system.
#' 2) "onehot": One-Hot encoding.
#' @param x.normalization: One of available normalization types for X values:
#' 1) "NA": Not Assigned (none).
#' 2) "AM": Abs-Max normalization
#' 3) "MM": Min-Max normalization
#' 4) "AVG": Divide-by-average normalization
#' 5) "Z": Z-normalization
#' @param y.normalization: One of available normalization types for Y values:
#' 1) "NA": Not Assigned (none).
#' 2) "AM": Abs-Max normalization
#' 3) "MM": Min-Max normalization
#' 4) "AVG": Divide-by-average normalization
#' 5) "Z": Z-normalization
#' @param internal.validation: Fraction of examples to use for internal validation of deep models.
#' @param internal.iterations: Maximum number of training iterations.
#' @param internal.patience: Number of iterations for early stopping.
#' @param internal.batch: Batch size for each training iteration.
#' @param external.validation: Fraction of examples to use for external validation of shallow models (e.g. SVR).
#' @param external.optimizer: NULL or hyperparameter optimizer from metaOpt package (e.g. GOA).
#' @param external.population: Optimizer population size.
#' @param external.generations: Maximum number of optimization iterations.
#' @param external.optparam: Other optimizer parameters.
#' @param rep: The number of times to repeat tests (DeepAR-specific).
#' @param cooldown: The number of tests to perform before updating forecast model.
#' @param verbose: Shows whether or not to log details.
#' @param folder: Model export folder.
#' @param name: Model name to use for export.
#' @param ...: Model-specific hyperparameters (for TrainDNN/SVR function).
#' @return Y forecast.
ForecastTest = function(type = 'DCL',
                        data,
                        col_time,
                        cols_x_required,
                        cols_x_optional,
                        cols_y,
                        horizon,
                        lookback,
                        splitter,
                        uncertainty = 0,
                        encoding = 'polar',
                        x.normalization = 'MM',
                        y.normalization = ifelse(type=='DMN' || type=='DEL' || type == 'DeepAR', 'NA', 'MM'),
                        internal.validation = 0,
                        internal.iterations = 200,
                        internal.patience = 20,
                        internal.batch = 128,
                        external.validation = 0.2,
                        external.optimizer = NULL,
                        external.population = 10,
                        external.generations = 10,
                        external.optparam = list(),
                        rep = 25,
                        cooldown = 30,
                        verbose = FALSE,
                        folder = NULL,
                        name = type,
                        ...) {
    ExtValidations = function() return(ifelse(is.null(external.optimizer), 0, round(splitter / horizon * external.validation)))
    TrainTo = function() return(splitter - ExtValidations() * horizon)
    ExtValidateFrom = function() return(TrainTo() + 1)
    TestFrom = function() return(splitter + 1)
    SetNames = function(forecast) {
        GetNames = function() {
            if (ncol(forecast) != Count(cols_y)) return(c())
            if (Count(colnames(data)) == 0) return(cols_y)
            return(colnames(data)[cols_y])
        }

        colnames(forecast) = GetNames()
        return(forecast)
    }

    ArgsFile = function() return(paste(name, "args", sep = "-"))
    SaveArgs = function(args) {
        if (!is.null(folder)) args %>% ExportCSV(folder, ArgsFile())
    }
    LoadArgs = function() {
        Load = function() return(as.list(ImportCSV(folder, ArgsFile())))
        if (is.null(folder)) return(list())
        tryCatch({ return(Load()) }, error = function(e){ return(list()) })
    }
    SaveModel = function(model) {
        if (is.null(folder)) return(c())
        if (type == 'SVR') {
            model %>% SaveSVR(folder, name)
        } else {
            model %>% SaveDNN(folder, name, type)
        }
    }
    LoadModel = function() {
        if (is.null(folder)) return(NULL)
        if (type == 'SVR') return(LoadSVR(folder, name))
        return(LoadDNN(folder, name, type))
    }

    CreateDNN = function(x.count = NA, y.count = NA) {
        if (type == "DeepAR") return(DeepAR(horizon, lookback, internal.iterations, internal.patience, internal.batch, ...))
        if (type == "DMN") return(DMN(x.count, y.count, horizon, lookback, ...))
        if (type == "DEL") return(DEL(x.count, y.count, horizon, lookback, ...))
        return(DCL(x.count, y.count, horizon, lookback, ...))
    }
    Train = function(set) {
        TrainModel = function() {
            if (type == 'SVR') {
                return(TrainSVR(set$train$x, set$train$y, horizon, lookback, ...))
            } else {
                dnn = CreateDNN(ncol(set$train$x), ncol(set$train$y))
                if (type == "DeepAR") return(dnn %>% TrainDNN(set$train$t, set$train$y, horizon, lookback, type))
                return(dnn %>% TrainDNN(set$train$x, set$train$y, horizon, lookback, type,
                                        internal.validation, internal.iterations, internal.patience, internal.batch,
                                        verbose = verbose))
            }
        }

        model = TrainModel()
        model %>% SaveModel()
        return(model)
    }
    Test = function(model, set, test.indexes) {
        TestModel = function(t, x, y) {
            if (type == 'SVR') return(model %>% TestSVR(x, y, horizon, lookback))
            if (type == 'DeepAR') return(model %>% TestDNN(t, y, horizon, lookback, type, rep))
            return(model %>% TestDNN(x, y, horizon, lookback, type, rep))
        }

        forecast = data.frame()
        for (i in test.indexes) {
            test = set$test[[i]]
            normalized = TestModel(test$t, test$x, test$y)
            denormalized = Norm(normalized, set$norm$y, FALSE)
            forecast = Union(forecast, denormalized)
        }

        return(forecast)
    }
    Update = function(model, set, test.indexes, args = list()) {
        JoinTestData = function() {
            if (type == "SVR") {
                t = set$train$t
                x = set$train$x
                y = set$train$y
                for (i in test.indexes) {
                    test = set$test[[i]]
                    rows = (Count(test$x)-horizon+1):Count(test$x)
                    t = Union(t, test$t %>% Subset(rows = rows))
                    x = Union(x, test$x %>% Subset(rows = rows))
                    y = Union(y, test$y %>% Subset(rows = rows))
                }

                return(list(t=t, x=x,y=y))
            } else {
                t = c()
                x = c()
                y = c()
                for (i in test.indexes) {
                    test = set$test[[i]]
                    if (Count(x) == 0) {
                        t = test$t
                        x = test$x
                        y = test$y
                    } else {
                        rows = (Count(test$x)-horizon+1):Count(test$x)
                        t = Union(t, test$t %>% Subset(rows = rows))
                        x = Union(x, test$x %>% Subset(rows = rows))
                        y = Union(y, test$y %>% Subset(rows = rows))
                    }
                }

                return(list(t=t, x=x,y=y))
            }
        }
        UpdateDNN = function(joined) {
            if (type == "DeepAR") return(model %>% TrainDNN(joined$t, joined$y, horizon, lookback, type))
            return(model %>% TrainDNN(joined$x, joined$y, horizon, lookback, type,
                                      internal.validation, internal.iterations, internal.patience, internal.batch,
                                      verbose = verbose))
        }
        RetrainSVR = function(joined) {
            if (Count(args) == 0) {
                return(TrainSVR(joined$x, joined$y, horizon, lookback, ...))
            } else {
                return(TrainSVR(joined$x,
                                joined$y,
                                horizon,
                                lookback,
                                args$nu,
                                args$gamma,
                                args$cost,
                                args$tolerance))
            }
        }

        joined = JoinTestData()
        if (type == 'SVR') {
            return(RetrainSVR(joined))
        } else {
            return(UpdateDNN(joined))
        }
    }
    TrainOptimal = function(set) {
        best.model = NULL
        best.args = c()
        best.error = NA

        Errors = function(from, forecast) {
            to = from + Count(forecast) - 1
            actual = data[from:to, cols_y]
            errors = ForecastEval(actual, forecast)
            return(errors)
        }
        Evaluate = function(model, args) {
            forecast = model %>% Test(set, 1:ExtValidations())
            errors = Errors(ExtValidateFrom(), forecast)
            error = errors["CV"]
            if (is.na(best.error) || error < best.error) {
                best.args <<- args
                best.model <<- model
                best.error <<- error

                model %>% SaveModel()
                c(args, errors) %>% SaveArgs()
            }

            return(error)
        }
        EvaluateSVR = function(args) {
            names(args) = c("nu", "gamma", "cost", "tolerance")
            args = as.list(args)
            model = TrainSVR(set$train$x,
                             set$train$y,
                             horizon,
                             lookback,
                             args$nu,
                             args$gamma,
                             args$cost,
                             args$tolerance)
            return(model %>% Evaluate(args))
        }
        OptimizeSVR = function() {
            # nu, gamma, cost, tolerance:
            Optimize(EvaluateSVR,
                     c(0, 0, 0.5, 0),
                     c(0.1, 0.1, 1, 0.1),
                     external.optimizer,
                     external.population,
                     external.generations,
                     external.optparam)
        }
        UpdateModel = function() {
            best.model <<- best.model %>% Update(set, 1:ExtValidations())
            best.model %>% SaveModel()
        }

        if (type == "SVR") {
            OptimizeSVR()
            UpdateModel()
        }

        return(list(model = best.model, args = best.args, error = best.error))
    }

    PrepareSet = function() {
        set = ForecastSet(data,
                          col_time,
                          cols_x_required,
                          cols_x_optional,
                          cols_y,
                          horizon,
                          lookback,
                          TrainTo(),
                          uncertainty,
                          encoding,
                          x.normalization,
                          y.normalization)
        if (verbose) View(set)
        return(set)
    }
    PrepareModel = function(set) {
        best = list(args = LoadArgs(), model = LoadModel(), error = NA)
        if (!is.null(best$model)) {
            Log(c(type, " model loaded."))
            return(best)
        }
        if (!is.null(external.optimizer)) {
            watch = Log(c(external.optimizer, "-", type, " training started (", external.generations, "x", external.population, " iterations)..."))
            best = set %>% TrainOptimal()
            Log(c(external.optimizer, "-", type, " training finished (duration = ", Elapsed(watch), " sec)."))
            if (verbose) print(best$args)
            return(best)
        }

        watch = Log(c(type, " training started..."))
        best$model = set %>% Train()
        Log(c(type, " training finished (duration = ", Elapsed(watch), " sec)."))
        return(best)
    }
    ApplyModel = function(model, set, args) {
        watch = Log(c(type, " testing and updating started..."))
        forecast = data.frame()
        testing = 0

        snapshot = 0
        countdown = cooldown
        for (i in (ExtValidations()+1):Count(set$test)) {
            testwatch = Stopwatch()
            forecast = Union(forecast, model %>% Test(set, i))
            testing = testing + Elapsed(testwatch)

            countdown = countdown - 1;
            if (countdown <= 0) {
                snapshot = snapshot + 1
                if (verbose) Log(c(type, " update ", snapshot))
                model = model %>% Update(set, (i-cooldown+1):i, args)
                countdown = cooldown
            }
        }

        updating = Elapsed(watch) - testing
        Log(c(type, " testing and updating finished (duration = ", testing, " + ", updating, " sec)."))
        return(forecast %>% SetNames())
    }
    SaveForecast = function(forecast) {
        if (!is.null(folder)) forecast %>% ExportCSV(folder, name)
        if (verbose) View(forecast)
    }

    set = PrepareSet()
    best = set %>% PrepareModel()
    forecast = best$model %>% ApplyModel(set, best$args)
    forecast %>% SaveForecast()
    return(forecast)
}
