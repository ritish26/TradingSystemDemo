#!/bin/bash

VAULT_ADDR="http://localhost:8200"

echo "=========================================="
echo " Vault Setup Script"
echo "=========================================="

# Wait for Vault to be ready
echo "Waiting for Vault to be ready..."
until curl -sf "$VAULT_ADDR/v1/sys/health?standbyok=true&uninitcode=200&sealedcode=200" > /dev/null; do
  sleep 2
done
echo "Vault is ready."

# Step 1 — Check if already initialised
INIT_STATUS=$(curl -s "$VAULT_ADDR/v1/sys/init" | grep -o '"initialized":[^,}]*' | cut -d: -f2)

if [ "$INIT_STATUS" = "true" ]; then
  echo "Vault already initialised. Skipping init."
  echo "Please provide your root token:"
  read -s VAULT_TOKEN
else
  # Step 2 — Initialise Vault
  echo "Initialising Vault..."
  INIT_RESPONSE=$(curl -s \
    --request POST \
    --data '{"secret_shares":1,"secret_threshold":1}' \
    "$VAULT_ADDR/v1/sys/init")

  # Extract unseal key and root token
  UNSEAL_KEY=$(echo $INIT_RESPONSE | grep -o '"keys_base64":\["[^"]*"' | cut -d'"' -f4)
  VAULT_TOKEN=$(echo $INIT_RESPONSE | grep -o '"root_token":"[^"]*"' | cut -d'"' -f4)

  echo "Unseal Key: $UNSEAL_KEY"
  echo "Root Token: $VAULT_TOKEN"

  # Save them — you will need unseal key after every restart
  cat > vault-init-keys.txt <<EOF
UNSEAL_KEY=$UNSEAL_KEY
ROOT_TOKEN=$VAULT_TOKEN
EOF
  echo "Keys saved to vault-init-keys.txt"
  echo "IMPORTANT: Keep this file safe. Add to .gitignore."

  # Step 3 — Unseal Vault
  echo "Unsealing Vault..."
  curl -s \
    --request PUT \
    --data "{\"key\":\"$UNSEAL_KEY\"}" \
    "$VAULT_ADDR/v1/sys/unseal" > /dev/null
  echo "Vault unsealed."
fi

# Step 4 — Enable Transit
echo "Enabling Transit engine..."
curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request POST \
  --data '{"type":"transit"}' \
  "$VAULT_ADDR/v1/sys/mounts/transit" > /dev/null
echo "Transit enabled."

# Step 5 — Create JWT signing key
echo "Creating jwt-signing-key..."
curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request POST \
  --data '{"type":"rsa-2048","auto_rotate_period":"720h"}' \
  "$VAULT_ADDR/v1/transit/keys/jwt-signing-key" > /dev/null
echo "Key created."

# Step 6 — Enable AppRole
echo "Enabling AppRole..."
curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request POST \
  --data '{"type":"approle"}' \
  "$VAULT_ADDR/v1/sys/auth/approle" > /dev/null
echo "AppRole enabled."

# Step 7 — Auth Service policy
curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request PUT \
  --data '{"policy":"path \"transit/sign/jwt-signing-key\" { capabilities = [\"create\",\"update\"] } path \"transit/keys/jwt-signing-key\" { capabilities = [\"read\"] }"}' \
  "$VAULT_ADDR/v1/sys/policies/acl/auth-service-policy" > /dev/null
echo "Auth Service policy created."

# Step 8 — Order Service policy
curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request PUT \
  --data '{"policy":"path \"transit/keys/jwt-signing-key\" { capabilities = [\"read\"] }"}' \
  "$VAULT_ADDR/v1/sys/policies/acl/order-service-policy" > /dev/null
echo "Order Service policy created."

# Step 9 — Create AppRoles
curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request POST \
  --data '{"token_policies":["auth-service-policy"],"token_ttl":"1h"}' \
  "$VAULT_ADDR/v1/auth/approle/role/auth-service" > /dev/null
echo "Auth Service AppRole created."

curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request POST \
  --data '{"token_policies":["order-service-policy"],"token_ttl":"1h"}' \
  "$VAULT_ADDR/v1/auth/approle/role/order-service" > /dev/null
echo "Order Service AppRole created."

# Step 10 — Fetch credentials
AUTH_ROLE_ID=$(curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  "$VAULT_ADDR/v1/auth/approle/role/auth-service/role-id" \
  | grep -o '"role_id":"[^"]*"' | cut -d'"' -f4)

AUTH_SECRET_ID=$(curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request POST \
  "$VAULT_ADDR/v1/auth/approle/role/auth-service/secret-id" \
  | grep -o '"secret_id":"[^"]*"' | cut -d'"' -f4)

ORDER_ROLE_ID=$(curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  "$VAULT_ADDR/v1/auth/approle/role/order-service/role-id" \
  | grep -o '"role_id":"[^"]*"' | cut -d'"' -f4)

ORDER_SECRET_ID=$(curl -s \
  --header "X-Vault-Token: $VAULT_TOKEN" \
  --request POST \
  "$VAULT_ADDR/v1/auth/approle/role/order-service/secret-id" \
  | grep -o '"secret_id":"[^"]*"' | cut -d'"' -f4)

# Step 11 — Print results
echo ""
echo "=========================================="
echo " Setup Complete — Copy into appsettings"
echo "=========================================="
echo ""
echo "Auth Service appsettings.json:"
echo "  \"Vault\": {"
echo "    \"Address\": \"http://trading-vault:8200\","
echo "    \"KeyName\": \"jwt-signing-key\","
echo "    \"RoleId\": \"$AUTH_ROLE_ID\","
echo "    \"SecretId\": \"$AUTH_SECRET_ID\""
echo "  }"
echo ""
echo "Order Service appsettings.json:"
echo "  \"Vault\": {"
echo "    \"Address\": \"http://trading-vault:8200\","
echo "    \"KeyName\": \"jwt-signing-key\","
echo "    \"RoleId\": \"$ORDER_ROLE_ID\","
echo "    \"SecretId\": \"$ORDER_SECRET_ID\""
echo "  }"