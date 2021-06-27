source("https://raw.githubusercontent.com/igormanojlovic/TimeSeriesML/master/TimeSeriesML.R")

# Example 1: Run Time Series Grouping Algorithm (TSGA) on prepared UMass data:
data = read.csv("https://raw.githubusercontent.com/igormanojlovic/TimeSeriesML/master/DATA/UMass/ANDLP.csv", sep = "|", header = TRUE)
tsga = data %>% Pivot("Apartment", c("CharacteristicDay", "CharacteristicPeriod")) %>% Group(3, 10, cvi = 'CHI')
View(tsga$groups)

# Example 2: Run Deep Centroid Learning (DCL) regression on prepared UMass data:
data = read.csv("https://raw.githubusercontent.com/igormanojlovic/TimeSeriesML/master/DATA/UMass/HMLPSA.csv", sep = "|", header = TRUE)
dcl1 = ForecastTest("DCL", data, 1, ncol(data), ncol(data)-(7:1), 2:3, 24, 72, 365*24)
dcl2 = ForecastTest("DCL", data, 1, ncol(data), ncol(data)-(7:1), 4:5, 24, 72, 365*24)
forecast = Join(dcl1, dcl2)
View(forecast)
