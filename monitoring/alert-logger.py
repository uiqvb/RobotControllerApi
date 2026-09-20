"""
SIT223 7.3HD - notification sink for Alertmanager.

Alertmanager routing is only half a notification path: something has to receive
the dispatch and act on it. In production that receiver is email, PagerDuty or
Slack. None of those can be demonstrated on a machine with no outbound mail and
no shared workspace, and pointing the webhook at a URL that discards the payload
would mean the pipeline claims an alerting capability it does not have.

This is a real receiver. It accepts Alertmanager's webhook POST, parses the
payload and writes one line per alert to stdout, so `docker logs
robot-alert-logger` is evidence that the alert was actually delivered and not
merely raised. Swapping it for a real channel is a change of URL in
alertmanager.yml and nothing else.

Standard library only - no packages to install, nothing to pin.
"""

import datetime
import http.server
import json

PORT = 5001


def timestamp():
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%d %H:%M:%SZ")


class AlertHandler(http.server.BaseHTTPRequestHandler):

    def do_POST(self):
        length = int(self.headers.get("Content-Length", 0))
        raw = self.rfile.read(length).decode("utf-8", errors="replace")

        try:
            payload = json.loads(raw)
        except json.JSONDecodeError:
            print(f"[{timestamp()}] malformed payload: {raw}", flush=True)
            self._ok()
            return

        status = payload.get("status", "unknown").upper()
        receiver = payload.get("receiver", "unknown")
        alerts = payload.get("alerts", [])

        print("", flush=True)
        print("=" * 72, flush=True)
        print(
            f"[{timestamp()}] NOTIFICATION DELIVERED"
            f"  status={status}  receiver={receiver}  alerts={len(alerts)}",
            flush=True,
        )

        for alert in alerts:
            labels = alert.get("labels", {})
            annotations = alert.get("annotations", {})

            name = labels.get("alertname", "?")
            severity = labels.get("severity", "?")
            environment = labels.get("environment", "-")
            state = alert.get("status", "?").upper()

            print(f"  {state}  {name}", flush=True)
            print(f"      severity    : {severity}", flush=True)
            print(f"      environment : {environment}", flush=True)

            if annotations.get("summary"):
                print(f"      summary     : {annotations['summary']}", flush=True)
            if annotations.get("runbook"):
                print(f"      runbook     : {annotations['runbook']}", flush=True)
            if alert.get("startsAt"):
                print(f"      started     : {alert['startsAt']}", flush=True)
            if state == "RESOLVED" and alert.get("endsAt"):
                print(f"      resolved    : {alert['endsAt']}", flush=True)

        print("=" * 72, flush=True)
        self._ok()

    def do_GET(self):
        # Lets the pipeline confirm the receiver is reachable before declaring
        # the monitoring stage healthy.
        self._ok(b'{"status":"ready","service":"alert-logger"}')

    def _ok(self, body=b"ok"):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, *args):
        # Suppress the default per-request access log; the alert lines above are
        # the only output worth reading.
        pass


if __name__ == "__main__":
    print(f"[{timestamp()}] alert-logger listening on :{PORT}", flush=True)
    http.server.HTTPServer(("", PORT), AlertHandler).serve_forever()
