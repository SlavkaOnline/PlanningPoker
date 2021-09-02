package Domain

sealed trait Validation {
  def errorMessage: String
}   

case object CardCantMovedMoreOneStep extends Validation {
    def errorMessage = "The card can only be moved one step"
}

case object NotEnoughPlayers extends Validation {
    def errorMessage = "Not enough players to start the game, min 2"
}

case object NotEnoughCards extends Validation {
    def errorMessage = "Not enough cards to start the game, min 2"
}

case object NotEnoughColumns extends Validation {
    def errorMessage = "Not enough columns to start the game, min 2"
}
