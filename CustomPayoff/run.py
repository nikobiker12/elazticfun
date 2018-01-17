import os
import json

postreqdata = json.loads(open(os.environ['req']).read())
response = open(os.environ['res'], 'w')
response.write("hello world from "+str(postreqdata['paths'][1][0]))
response.close()