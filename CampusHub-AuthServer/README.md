# CampusHub AuthServer

**The OpenID Connect provider for SDEV 2351 Module 2.** It runs on your own machine and serves
Days 13, 14, 16 and all of Lab 2. You install it once, in Week 5, and leave it running.

There is no account to create and no tenant to provision. Everything the course needs is already
registered inside the server.

> ### ⚠️ Unzip this OUTSIDE any repository you commit to
>
> `src/CampusHub.AuthServer/Seed/Clients.cs` contains the client secrets for every course
> application. If you unzip this folder inside your Lab 2 repository and commit it, you have
> committed a client secret.
>
> **In Lab 2 that is an automatic zero on the Architecture criterion (rubric item A11).**
>
> Put it somewhere separate — `C:\SDEV2351\CampusHub-AuthServer` or `~/SDEV2351/CampusHub-AuthServer`
> is fine. Anywhere that is not inside a folder where you run `git add`.

---

## Start it

```bash
dotnet run --project src/CampusHub.AuthServer --urls https://localhost:7300
```

**The `--project` argument is required.** There is no `.csproj` or `.sln` in this folder — only
`src/`. Running a bare `dotnet run` here fails with *"Couldn't find a project to run."*

**Check it worked.** Open this in a browser:

```
https://localhost:7300/.well-known/openid-configuration
```

You should get a page of JSON. That is the discovery document — the standard way an OIDC provider
publishes what it can do. If you get JSON, the server is running and correctly configured.

**Leave the console window visible.** When authentication fails, this server tells you why in
plain text. That is the main reason the course runs its own provider, and you lose it if you
minimize the window.

**Or open the Activity page:**

```
https://localhost:7300/Activity
```

It shows the same messages as the console, newest first, and refreshes every 3 seconds. Each
step of a sign-in appears as a row. A rejected request is highlighted, with the reason. Use
**Clear** before you try a sign-in, so that only that attempt is on the page.

---

## Reset it

```bash
dotnet run --project src/CampusHub.AuthServer -- --reset
```

This rebuilds the client registrations and scopes from the beginning, and then **starts the server
normally** — exactly as a plain `dotnet run` would. Run it if sign-in starts failing in a way you
cannot explain.

**The command does not return to the prompt when the reset finishes**, because the server is now
running. That is the expected result, not a hang. You do not need to start the server separately
afterwards. Press `Ctrl+C` when you want to stop it.

**Note the bare `--`.** It separates arguments to `dotnet run` from arguments to the application.
Without it, `dotnet` tries to interpret `--reset` itself and fails.

You do **not** need `--reset` if nothing changed. The fixture users live in memory, and the server
rebuilds them on every start.

---

## Sign in

Every fixture account uses the same password:

```
Campus#Hub2351
```

One shared password is appropriate here and nowhere else: there are no real people behind these
accounts and no real data behind the API. Contrast the **client secret** your application holds,
which goes in `dotnet user-secrets` and never in your repository.

### Lab 2 accounts — the six-role model

| Email | Roles |
|---|---|
| `records@sdev2351.local` | RecordsAccess |
| `notifications@sdev2351.local` | NotificationAccess |
| `course-admin@sdev2351.local` | CourseAdmin |
| `library-staff@sdev2351.local` | LibraryStaff |
| `event-organizer@sdev2351.local` | EventOrganizer |
| `facility-manager@sdev2351.local` | FacilityManager |
| `multi-role@sdev2351.local` | all six |
| `no-role@sdev2351.local` | none |

**`no-role@` is the most useful account in the list.** It is the only way to *see* that your
authorization actually blocks a request, instead of assuming it does from reading the code. Test
the deny path with it every time.

### Day 13, 14 and 16 accounts

| Email | Roles | Department | Used for |
|---|---|---|---|
| `demo@sdev2351.local` | none | — | Days 13 and 14 — the sign-in handshake and claims |
| `admin@sdev2351.local` | Admin | Inventory | Day 16 — passes the reports policy by **role** |
| `manager@sdev2351.local` | Manager | Inventory | Day 16 — the negative case; passes neither branch |
| `analyst@sdev2351.local` | Manager | Analytics | Day 16 — passes the reports policy by **department** |

Day 16 uses these three to show a policy with an OR in it. `admin@` and `analyst@` both see the
reports; `manager@` reaches the same page and does not. The only thing that explains the
difference is the department.

---

## Registered applications

You do not need to configure any of these. They are here so you can see what a provider actually
stores about a client — and because Day 13 has you read this file.

| Client ID | Application | Redirect URI |
|---|---|---|
| `campushub-lab2` | Lab 2 | `https://localhost:5001/callback` |
| `day13-practice` | Day 13 demo and practice | `https://localhost:5131` / `:5132` |
| `day14-practice` | Day 14 demo and practice | `https://localhost:5141` / `:5142` |
| `day16-demo` | Day 16 demo and practice | `https://localhost:5161` / `:5162` |

Registrations live in `src/CampusHub.AuthServer/Seed/Clients.cs`. **Edit that file and you must
run `--reset`** — the server stores client registrations in the database, unlike the fixture users.

---

## If something goes wrong

| Symptom | Cause |
|---|---|
| *"Couldn't find a project to run."* | Missing `--project src/CampusHub.AuthServer`. |
| Browser warns the certificate is not trusted | Run `dotnet dev-certs https --trust`. It is per-user and needs no admin rights. |
| Your app cannot reach the server, but the browser can | **On Linux or WSL only:** export `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"` before starting your client, or its back-channel call fails closed. |
| Port 7300 already in use | The server is already running in another window. |
| Sign-in fails after you edited `Clients.cs` | Run with `-- --reset`. |
| `401` mentioning *audience 'empty' is invalid* | A scope was registered without a resource. This one is worth reading about — it is a real OpenIddict trap the course documents. |

Full prerequisites, a walkthrough, and a longer troubleshooting list are in
**`Day-13/Guide/AuthServer-Setup-Guide.md`**.

---

## What this server is, and is not

It is a **real** OpenID Connect provider. It enforces PKCE, issues signed JWT access and ID
tokens, rotates refresh tokens, and publishes a standard discovery document. The code you write
against it is standard OIDC client code, which is why it would work against any standards-compliant
OIDC provider with a URL change.

It is **not** production software. The user store is a dictionary, the passwords are shared, and
there is no MFA, no password reset, and no rate limiting. That is deliberate — it is built to be
*read*, and everything it does that matters is the real thing.

Built with [OpenIddict](https://documentation.openiddict.com/) 7.6.0.

---

*Last updated: 2026-10-05 — student run and reset card for the CampusHub AuthServer.*
