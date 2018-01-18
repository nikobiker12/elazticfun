import os
import json
import math

def geometricAverage(strike, spots, callOrPut):
	nbPaths = len(spots)
	sum = [1.0] * nbPaths
	for j in range(0, nbPaths):
		nbDates = len(spots[j])
		sumTemp = 1.0
		for i in range(0, nbDates):
			sumTemp = sumTemp * spots[j][i]
		sum[j] = max(callOrPut * (math.pow(sumTemp, 1/nbDates) - strike), 0.0)
	return sum

def arithmeticAverage(strike, spots, callOrPut):
	nbPaths = len(spots)
	sum = [0.0] * nbPaths
	for j in range(0, nbPaths):
		nbDates = len(spots[j])
		sumTemp = 0.0
		for i in range(0, nbDates):
			sumTemp = sumTemp + spots[j][i]
		sum[j] = max(callOrPut * (sumTemp / nbDates - strike), 0.0)
	return sum




postreqdata = json.loads(open(os.environ['req']).read())
response = open(os.environ['res'], 'w')
payoffList = arithmeticAverage(postreqdata['strike'], postreqdata['paths'], postreqdata['direction'])
response.write(str(payoffList))
response.close()






