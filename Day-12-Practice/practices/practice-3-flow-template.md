# Practice 3: OIDC Authorization Code Flow Trace

**Day 12 -- OIDC Overview and Claims-Based Identity**
**Time:** 15 minutes | **Type:** Independent

---

## Instructions

The OIDC Authorization Code Flow with PKCE has 8 steps, numbered the same as today's flow diagram. Four steps are filled in for you. Complete the missing steps (2, 4, 6, and 8), then answer the failure scenario questions.

---

## The Flow

```
  Browser / Blazor App                    Identity Provider
  =====================                    ========================

  Step 1: User clicks "Login"
  App generates code_verifier and
  code_challenge (PKCE)
          |
          |
          v
  Step 2: ________________________________
          ________________________________
          ________________________________
          |
          |
          v
  Step 3: User enters credentials at
  the Identity Provider's login page
  (NOT your app's page)
          |
          |
          v
  Step 4: ________________________________
          ________________________________
          ________________________________
          |
          |
          v
  Step 5: Browser delivers the code to
  the app's callback URL
          |
          |
          v
  Step 6: ________________________________
          ________________________________
          ________________________________
          |
          |
          v
  Step 7: Identity Provider returns
  ID Token + Access Token
          |
          |
          v
  Step 8: ________________________________
          ________________________________
          ________________________________
```

---

## Fill In the Missing Steps

**Step 2:** What does the app do after generating the PKCE values?

> _______________________________________________
>
> _______________________________________________

**Step 4:** What does the Identity Provider do after the user successfully authenticates?

> _______________________________________________
>
> _______________________________________________

**Step 6:** What does the app do with the value it received in Step 5?

> _______________________________________________
>
> _______________________________________________

**Step 8:** How does the app use the tokens it received in Step 7?

> _______________________________________________
>
> _______________________________________________

---

## Failure Scenarios

**Scenario A:** The user enters the **wrong password** at Step 3. What happens?

> _______________________________________________
>
> _______________________________________________

**Scenario B:** An attacker **intercepts the authorization code** returned in Step 4. Can they get the user's tokens? Why or why not?

> _______________________________________________
>
> _______________________________________________

**Scenario C:** The app is using the tokens from Step 7, but the **access token expires**. What happens when the app tries to call an API?

> _______________________________________________
>
> _______________________________________________

---

## Key Terms to Use in Your Answers

- `code_verifier` / `code_challenge` (PKCE)
- authorization code (one-time use)
- ID token / access token
- redirect
- 401 Unauthorized
- refresh token
