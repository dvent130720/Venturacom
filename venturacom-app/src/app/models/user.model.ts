export interface User {
  id?: string;
  email: string;
  name?: string;
  photoUrl?: string;
  provider: 'email' | 'google';
  token?: string;
}
