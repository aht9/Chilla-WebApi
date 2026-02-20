for run Redis Docker Use this command :
docker run --name chilla-redis -p 6379:6379 -d redis
docker start chilla-redis

 * Add Command for redis container in Docker:
 docker run --name chilla-redis -d \
  -p 6379:6379 \
  redis redis-server --maxmemory 512mb --maxmemory-policy allkeys-lru
  