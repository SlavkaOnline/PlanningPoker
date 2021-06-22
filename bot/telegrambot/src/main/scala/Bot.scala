import cats.instances.future._
import cats.syntax.functor._
import com.bot4s.telegram.api.RequestHandler
import com.bot4s.telegram.api.declarative.Commands
import com.bot4s.telegram.clients.{FutureSttpClient, ScalajHttpClient}
import com.bot4s.telegram.future.{Polling, TelegramBot}
import sttp.client3.okhttp.OkHttpFutureBackend

import scala.util.Try
import scala.concurrent.Future
import com.microsoft.signalr.HubConnectionBuilder
import io.reactivex.Single

class Bot(val token: String) extends TelegramBot
    with Polling
    with Commands[Future] {


    // Or just the scalaj-http backend
    override val client: RequestHandler[Future] = new ScalajHttpClient(token)

    val rng = new scala.util.Random(System.currentTimeMillis())
    onCommand("foo") { implicit msg =>
        withArgs(args => {
            val session = args.head
            val token = args(1)
            val hub = HubConnectionBuilder.create("http://planningpocker.azurewebsites.net/events")
                .withAccessTokenProvider(Single.defer( () => Single.just(token)))
                .build()
            hub.start().blockingAwait()
            hub.stream(classOf[Any], "session", session, 0)
                .subscribe(
                    (item: Any) => {
                        msg.from match {
                            case Some(user) => {
                                println(item)
                                reply(item.toString).void
                            }
                            case None => reply("Just hello").void
                        }
                        ()
                    },
                    err => println(err),
                    () => println(""));

            reply(s"Connected").void
        }
        )
    }

}
