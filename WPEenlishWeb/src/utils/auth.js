const TOKEN_KEY = 'token'

export function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function setToken(token) {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken() {
  localStorage.removeItem(TOKEN_KEY)
}

function base64UrlDecode(str) {
  let s = str.replace(/-/g, '+').replace(/_/g, '/')
  const pad = s.length % 4
  if (pad === 2) s += '=='
  else if (pad === 3) s += '='
  const binary = atob(s)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
  return new TextDecoder('utf-8').decode(bytes)
}

export function decodeTokenPayload(token) {
  if (!token) return null
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return null
    return JSON.parse(base64UrlDecode(parts[1]))
  } catch (e) {
    return null
  }
}

export function getTokenExpiry(token = getToken()) {
  const payload = decodeTokenPayload(token)
  if (!payload || !payload.exp) return null
  return new Date(payload.exp * 1000)
}

export function isTokenExpired(token = getToken()) {
  if (!token) return true
  const expiry = getTokenExpiry(token)
  if (!expiry) return true
  return expiry.getTime() <= Date.now()
}

export function getTokenRemainingMs(token = getToken()) {
  const expiry = getTokenExpiry(token)
  if (!expiry) return 0
  return expiry.getTime() - Date.now()
}
