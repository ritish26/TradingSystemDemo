# Auth Service — Vault Setup Guide

## Overview

Auth Service uses HashiCorp Vault Transit Engine for JWT signing. The private key never leaves Vault — Vault signs internally and returns only the signature. This guide covers setup, common errors, and daily workflow.

---

## How to Set Up Vault — All Scenarios

### Do you need unseal-vault.sh?

No. `setup-vault.sh` handles unsealing automatically in all cases. You never need to run `unseal-vault.sh` separately.

---

### If you are making up the application run  ./setup-vault.sh 

```bash
docker-compose up -d       # start all containers
./setup-vault.sh           # initialises + unseals + creates everything
                           # copy printed RoleId + SecretId into docker-compose.yml
docker-compose up -d authservice   # restart auth service with new credentials
```
---

After this new RoleId + SecretId generated, need to put either in appsettings or docker-compose.yml and restart auth service.

---

## docker-compose.yml — Vault Section

```yaml
vault:
  image: hashicorp/vault:latest
  container_name: trading-vault
  ports:
    - "8200:8200"
  environment:
    VAULT_ADDR: http://0.0.0.0:8200
    VAULT_API_ADDR: http://0.0.0.0:8200
  volumes:
    - vault-data:/vault/data
    - ./vault-config.hcl:/vault/config/vault.hcl
  command: vault server -config=/vault/config/vault.hcl
  networks:
    - trading-network
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:8200/v1/sys/health?standbyok=true&uninitcode=200&sealedcode=200"]
    interval: 5s
    timeout: 3s
    retries: 10

volumes:
  vault-data:
```

## Redis Important Commands:


```
Login to Redis CLI
docker exec -it trading-redis redis-cli

# All token cache keys
keys token-cache:*

# TTL of specific key
TTL token-cache:{key}

# See token value
GET token-cache:{key}

# All rate limit counters
keys auth-rate-limit:*

# Check IP counter
GET auth-rate-limit:ip:::1

# Check username counter
GET auth-rate-limit:user:admin

# TTL of rate limit key
TTL auth-rate-limit:user:admin

# All whitelist IPs
SMEMBERS ip-whitelist
```


