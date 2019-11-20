import sys,getopt
import time
start=time.time()
opts,args=getopt.getopt(sys.argv[1:],"t:s:a:b:")
file=""
cache_size=0
ass=0
block_size=0
tag=0
succ=0
fail=0
index=0

for op, value in opts:
    if op=="-t":
        file=value
    elif op=="-s":
        cache_size=int(value)
    elif op=="-a":
        ass=int(value)
    elif op=="-b":
        block_size=int(value)

if file=="":
    print("error filename")
    sys.exit()
if cache_size>(65536) or cache_size<1024:
    print("error cache_size")
    sys.exit()
if block_size!=32 and block_size!=64 and block_size!=128 and block_size!=256:
    print("error block_size")
    sys.exit()
if ass>(cache_size/block_size):
    print("error associativity")

m=int(cache_size//block_size)
g=int(m//ass)
filename=file+".txt"
fp=open(filename,"r")
done=0
cache=[[-1 for col in range(ass)] for row in range(g)]

s=fp.readline()
d=int(s)
i=int(d//block_size)
k=i % g
#f=int(d//cache_size)
cache[k][0]=i
fail+=1

while not done:
    s=fp.readline()
    if(s!=""):
        
        d=d+int(s)
        i=int(d//block_size)
        k=i % g
        #f=int(d//cache_size)
        #判断是否命中
        for value in cache[k]:
            if value!=-1:
                index+=1
            if value==i:
                tag=1
                break
        #未命中调块
        if tag ==0:
            if index<ass:
                cache[k][index]=i
                fail+=1
            else:
                fail+=1
                for value in range(len(cache[k])):
                    if value<(len(cache[k])-1):
                        cache[k][value]=cache[k][value+1]
                    else:
                        cache[k][value]=i            
        else:
            succ+=1
        index=0
        tag=0
    else:
        done=1

print("program_name: go_ld_trace")
print("cache_size: ",cache_size,"B")
print("block_size: ",block_size)
print("associativity: ",ass)
print("total_lds: 1500000")
print("cache_hits: ",succ)
print("cache_misses: ",fail)
print("cache_miss_rate: ",fail/15000000)
        
end=time.time()
print("time is:", end-start,"s")




sys.exit()
