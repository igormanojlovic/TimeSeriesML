#' Z: Z value.
#' @param p: Probability level.
#' @return Z value.
Z = function(p) return(abs(qnorm((1-p)/2)))

#' CI: Confidence Interval.
#' @param m: Means.
#' @param sd: Standard deviations.
#' @param n: Sample size.
#' @param p: Probability level.
#' @return A list with the following elements:
#' 1) "lower": Lower confidence interval values.
#' 2) "upper": Upper confidence interval values.
CI = function(m, sd, n, p) {
    m = unlist(m)
    e = Z(p) * unlist(sd) * sqrt(1/n)
    return(list(lower = m-e, upper = m+e))
}

#' PI: Prediction Interval.
#' @param m: Means.
#' @param sd: Standard deviations.
#' @param n: Sample size.
#' @param p: Probability level.
#' @return A list with the following elements:
#' 1) "lower": Lower prediction interval values.
#' 2) "upper": Upper prediction interval values.
PI = function(m, sd, n, p) {
    m = unlist(m)
    e = Z(p) * unlist(sd) * sqrt(1+1/n)
    return(list(lower = m-e, upper = m+e))
}

#' MAE: Mean Absolute Error.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @return MAE.
MAE = function(y_true, y_pred) {
    y_true = unlist(y_true)
    y_pred = unlist(y_pred)
    return(mean(abs(y_pred - y_true)))
}

#' MAPE: Mean Absolute Percentage Error.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @return MAPE[%].
MAPE = function(y_true, y_pred) {
    y_true = unlist(y_true)
    y_pred = unlist(y_pred)
    return(100 * mean(abs((y_pred - y_true) / y_true)))
}

#' RMSE: Root Mean Square Error.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @return RMSE.
RMSE = function(y_true, y_pred) {
    y_true = unlist(y_true)
    y_pred = unlist(y_pred)
    return(sqrt(mean((y_pred - y_true) ^ 2)))
}

#' CVRMSE: Coefficient of Variation (CV) of the Root Mean Square Error (RMSE).
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @return CV(RMSE)[%].
CVRMSE = function(y_true, y_pred) return(100 * RMSE(y_true, y_pred) / mean(y_true))

#' ACE: Average Coverage Error.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @param sd: Expected standard deviations.
#' @param n: Sample size.
#' @param p: Probability level.
#' @return ACE[%].
ACE = function(y_true, y_pred, sd, n, p) {
    y_true = unlist(y_true)
    i = PI(y_pred, sd, n, p)
    covered = as.numeric(i$lower <= y_true & y_true <= i$upper)
    return(100 * (mean(covered)-p))
}

#' Winkler: Winkler loss
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @param sd: Expected standard deviations.
#' @param n: Sample size.
#' @param p: Probability level.
#' @return One winkler loss for each forecast step.
Winkler = function(y_true, y_pred, sd, n, p) {
    alpha = 1-p
    i = PI(y_pred, sd, n, p)
    d = data.frame(y = unlist(y_true)
                  ,l = i$lower
                  ,u = i$upper)
    d = d %>% mutate(e = (u-l) + ifelse(l <= y & y <= u, 0,
                                           ifelse(y < l, 2*(l-y)/alpha
                                                       , 2*(y-u)/alpha)))
    return(unlist(d[,"e"]))
}

#' WS: Winkler Score.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @param sd: Expected standard deviations.
#' @param n: Sample size.
#' @param p: Probability level.
#' @return WS.
WS = function(y_true, y_pred, sd, n, p) return(mean(Winkler(y_true, y_pred, sd, n, p)))

#' Pinball: Pinball loss.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @param sd: Expected standard deviations.
#' @param n: Sample size.
#' @param q: Number of quantiles.
#' @return Table of pinball losses for each forecast step (rows) at each quantile (columns).
Pinball = function(y_true, y_pred, sd, n, q = 100) {
    pinball_i = function(i) {
        d = data.frame( y = unlist(y_true)
                      ,yq = unlist(y_pred+sd*qnorm(i/q))
                      , q = rep(i/q, Count(y_true)))
        d = d %>% mutate(e = ifelse(y<yq, (1-q) * (yq-y)
                                        , q     * (y-yq)))
        return(d[,"e"])
    }

    p = data.frame()
    for (i in 1:(q-1)) {
        p = Join(p, pinball_i(i), colnames2=paste("Q", i, sep = ""))
    }

    return(p)
}

#' QS: Quantile Score.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @param sd: Expected standard deviations.
#' @param n: Sample size.
#' @param q: Number of quantiles.
#' @return QS
QS = function(y_true, y_pred, sd, n, q = 100) return(mean(unlist(Pinball(y_true, y_pred, sd, n, q))))

#' CRPS: Continuous Ranked Probability Score.
#' @param y_true: Actual values.
#' @param y_pred: Expected values.
#' @param sd: Expected standard deviations.
#' @return CRPS.
CRPS = function(y_true, y_pred, sd) {
    actual = unlist(y_true)
    expect = Join(unlist(y_pred), unlist(sd))
    return(crps(actual, expect)$CRPS)
}

#' DM: Diebold-Mariano (DM) test.
#' Performs DM test for each step in forecast horizon.
#' @param e1: Errors for model 1 for each step in one or more forecast horizons.
#' @param e2: Errors for model 2 for each step in one or more forecast horizons.
#' @param horizon: Forecast horizon.
#' @param p: Probability level.
#' @param power: The power used calculating errors (e.g. 1 for MAE, 2 for RMSE).
#' @return One value for each step in forecast horizon:
#' [1] if model 1 can be considered more accurate than model 2.
#' [-1] if model 2 can be considered more accurate than model 1.
#' [0] if there is no significant difference between the models.
DM = function(e1, e2, horizon, p = 0.95, power = 1) {
    Test = function(better, worse) {
        dm = dm.test(worse, better, alternative = 'g', h = horizon, power = power)
        return(dm$p.value < 1-p)
    }

    e1 = unlist(e1)
    e2 = unlist(e2)
    count = min(Count(e1), Count(e2))
    start = horizon*0:(count/horizon-1)+1

    v = c()
    for(i in 1:horizon) {
        range = start+i-1
        if (Test(e1[range], e2[range])) {
            v = c(v, 1)
        } else if (Test(e2[range], e1[range])) {
            v = c(v, -1)
        } else {
            v = c(v, 0)
        }
    }

    return(v)
}