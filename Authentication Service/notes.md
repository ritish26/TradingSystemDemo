## JWT Key Rotation — Notes

**Private key** signs the token, **public key** verifies it. Each key pair has a **kid (Key ID)** as a label to identify which pair was used.

When a token arrives, the verifier (order service) reads the **kid from JWT header** and picks the matching public key to verify.

**JWKS endpoint** (`/.well-known/jwks.json`) is hosted by Auth Service and exposes all public keys — so any service can automatically find the right public key using kid, without manual configuration.

**Key rotation** = generate new key pair → assign new kid → old tokens still valid until expiry → retire old kid. This limits damage if a key is ever leaked.

```
Private key  →  signs token  →  kid="key-2" in JWT header
JWKS endpoint →  exposes public keys  →  verifier picks key-2  →  verifies ✅
```

> Private keys never leave Auth Service. Public keys are shared openly via JWKS.

---

## Where to Store Private Keys — Vault

Instead of storing private keys on disk (risky), store them in **HashiCorp Vault** — a secrets management tool.

Auth Service fetches the private key from Vault at runtime → signs the token → kid goes into JWT header. The private key **never touches disk** and Vault maintains a full audit log of who accessed what and when.

To avoid hitting Vault on every token generation, the key is **cached in memory** and refreshed periodically (e.g. every 1 hour).

```
Vault (stores BOTH private + public keys securely)
        ↓
Auth Service fetches private key → signs token → kid in JWT header
        ↓
Order Service fetches public key from Vault → verifies ✅
```

> Vault adds access control, audit logging, and auto-rotation on top of your key management — making it production grade.

**Why store both in Vault?**
- Single source of truth for all keys 🔒
- Fine-grained access control — Auth Service can access private key, Order Service can only access public key
- Full audit log — who fetched which key and when
- Key rotation in one place — update Vault, all services pick up new key automatically
## Vault Key Rotation — Notes

Vault stores multiple keys (key-1, key-2, key-3) and can auto-generate/rotate them. 
Auth Service picks the **current active key based on time** (e.g. every hour) and puts the matching **kid in JWT header**.
Old keys are **kept in Vault until all their tokens expire** (e.g. 60 mins) so no valid token is ever rejected. 
Order Service reads kid from JWT → fetches matching public key from Vault → verifies.
This gives a **single source of truth** for all keys with full audit logging and automatic rotation — fully production grade. 