#!/bin/bash

VAULT_ADDR="http://localhost:8200"
UNSEAL_KEY=$(grep UNSEAL_KEY vault-init-keys.txt | cut -d= -f2)

echo "Unsealing Vault..."
curl -s \
  --request PUT \
  --data "{\"key\":\"$UNSEAL_KEY\"}" \
  "$VAULT_ADDR/v1/sys/unseal" > /dev/null
echo "Vault unsealed. Ready to accept connections."
