import type { NextApiRequest, NextApiResponse } from 'next';
import { authBridge } from '@/server/authBridge';
export default function handler(req: NextApiRequest, res: NextApiResponse) {
  return authBridge(req, res);
}
