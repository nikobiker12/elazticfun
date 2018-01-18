import os
import json
import math


def customPayoffPrice(payoffName, postreqdata):
	if(payOffName == "asian-average"):
		return arithmeticAverage(postreqdata['strike'], postreqdata['paths'], postreqdata['optionType'])	
	elif(payOffName == "geometric-average"):
		return geometricAverage(postreqdata['strike'], postreqdata['paths'], postreqdata['optionType'])
	elif(payoffName == "oneTouch"):
		return geometricAverage(postreqdata['strike'], postreqdata['paths'], postreqdata['optionType'])
	else:
		print("{} is not supported, please create our function".format(payoffName))
		return [0.0] * len(postreqdata['paths'])


def geometricAverage(strike, spots, optionType):
	nbPaths = len(spots)
	sum = [1.0] * nbPaths
	for j in range(0, nbPaths):
		nbDates = len(spots[j])
		sumTemp = 1.0
		for i in range(0, nbDates):
			sumTemp = sumTemp * spots[j][i]
		sum[j] = max(optionType * (math.pow(sumTemp, 1/nbDates) - strike), 0.0)
	return sum


def arithmeticAverage(strike, spots, optionType):
	nbPaths = len(spots)
	sum = [0.0] * nbPaths
	for j in range(0, nbPaths):
		nbDates = len(spots[j])
		sumTemp = 0.0
		for i in range(0, nbDates):
			sumTemp = sumTemp + spots[j][i]
		sum[j] = max(optionType * (sumTemp / nbDates - strike), 0.0)
	return sum


def oneTouch(strike, spots):
	nbPaths = len(spots)
	sum = [0.0] * nbPaths
	for j in range(0, nbPaths):
		if(spots[len(spots[j])] >= strike):
			sum[j] = 1
	return sum


postreqdata = json.loads(open(os.environ['req']).read())
response = open(os.environ['res'], 'w')
response.write(str(customPayoffPrice(payoffName, postreqdata)))
response.close()






