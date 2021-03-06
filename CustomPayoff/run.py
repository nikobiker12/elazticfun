import os
import json
import math


def customPayoffPrice(payoffName, postreqdata):
	if(payoffName == "asian-arithmetic"):
		return arithmeticAverage(postreqdata['strike'], postreqdata['paths'], postreqdata['optionType'])
	elif(payoffName == "asian-geometric"):
		return geometricAverage(postreqdata['strike'], postreqdata['paths'], postreqdata['optionType'])
	elif(payoffName == "oneTouch"):
		return oneTouch(postreqdata['strike'], postreqdata['paths'])
	else:
		print("Payoff {} is not supported, prices will be 0".format(payoffName))
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
response.write(str(customPayoffPrice(postreqdata['payoffName'], postreqdata)))
response.close()






