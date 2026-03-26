export interface User {
  id:        string;
  email:     string;
  name:      string | null;
  avatarUrl: string | null;
  provider:  'email' | 'google';
}

export interface AuthResponse {
  accessToken:  string;
  refreshToken: string;
  expiresAt:    string;
  user:         User;
  isNewUser:    boolean;
}

export type LoginStep = 'initial' | 'otp';
