import { User, UserManager, WebStorageStateStore } from "oidc-client-ts";

const authority = import.meta.env.VITE_OIDC_AUTHORITY ?? "http://localhost:8080/realms/atlas";
const clientId = import.meta.env.VITE_OIDC_CLIENT_ID ?? "atlas-web";

export const userManager = new UserManager({
  authority,
  client_id: clientId,
  redirect_uri: `${window.location.origin}/auth/callback`,
  post_logout_redirect_uri: window.location.origin,
  response_type: "code",
  scope: "openid profile email roles",
  automaticSilentRenew: true,
  userStore: new WebStorageStateStore({ store: window.localStorage })
});

export async function getUser(): Promise<User | null> {
  return userManager.getUser();
}

export async function getAccessToken(): Promise<string | null> {
  const user = await getUser();
  if (!user || user.expired) {
    return null;
  }

  return user.access_token;
}

export async function signIn(): Promise<void> {
  await userManager.signinRedirect();
}

export async function completeSignIn(): Promise<void> {
  await userManager.signinRedirectCallback();
}

export async function signOut(): Promise<void> {
  await userManager.signoutRedirect();
}
