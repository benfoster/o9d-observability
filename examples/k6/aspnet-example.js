import http from 'k6/http';
import { check } from 'k6';

export let options = {
  vus: 1,
  duration: '1m',
  insecureSkipTLSVerify: true
};

export default function () {
  let res = http.get('https://localhost:5001/demo/status/200');
  check(res, {
    'is status 200': (r) => r.status === 200,
  });
}